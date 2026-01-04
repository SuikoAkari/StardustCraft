using StardustCraft.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
namespace StardustCraft.Server;



public class NetServer
{
    private TcpListener _listener;
    private readonly List<ClientConnection> _clients = new();
    public World world;

    public async Task StartAsync(int port = 25565)
    {
        world = new();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        Console.WriteLine($"[SERVER] Listening on port {port}");

        while (true)
        {
            var tcpClient = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("[SERVER] Client connected");

            var client = new ClientConnection(tcpClient);
            lock (_clients) _clients.Add(client);

            _ = client.RunAsync(OnPacketReceived, () =>
            {
                lock (_clients) _clients.Remove(client);
                Console.WriteLine("[SERVER] Client disconnected");
            });
        }
    }

    private void OnPacketReceived(ClientConnection client, NetPacket packet)
    {
        Console.WriteLine($"[SERVER] {packet.MsgId} ({packet.Payload.Length} bytes)");
        if(packet.MsgId == MsgId.CsPlayerLogin)
        {
            CsPlayerLogin req = CsPlayerLogin.Parser.ParseFrom(packet.Payload);
            ScPlayerLogin rsp = new ScPlayerLogin()
            {
                Uid = 1,
                Username = "Test"
            };
            _ = client.SendAsync(MsgId.ScPlayerLogin, rsp);
        }
        if(packet.MsgId == MsgId.CsAskChunkData)
        {
            CsAskChunkData req = CsAskChunkData.Parser.ParseFrom(packet.Payload);
            Console.WriteLine("sending chunk");
            ScAskChunkData rsp = world.GetChunk(req.X,req.Z).ToProto();
            _ = client.SendAsync(MsgId.ScAskChunkData, rsp);
        }
    }
}

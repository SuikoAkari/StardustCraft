using Google.Protobuf;
using StardustCraft.Protocol;
using StardustCraft.World;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace StardustCraft.Network;
public class NetManager
{
    private Socket ClientSocket;

    private readonly byte[] _recvBuffer = new byte[8192];
    private int _recvCount = 0;

    public bool Connected => ClientSocket?.Connected ?? false;

    public async Task ConnectAsync(string host, int port)
    {
        ClientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        await ClientSocket.ConnectAsync(host, port);

        _ = Task.Run(ReceiveLoop);
        await SendAsync(MsgId.CsPlayerLogin, new CsPlayerLogin() { ClientVersion = Game.ClientVersion, Token = "test" });
    }

    // ================= SEND =================
    public async Task SendAsync(NetPacket packet)
    {
        if (!Connected)
            return;

        var data = packet.ToBytes();
        await ClientSocket.SendAsync(data);
    }
    public async Task SendAsync(MsgId id, IMessage data)
    {
       await SendAsync(new NetPacket() { MsgId=id,Payload=data.ToByteArray() });
    }
    // ================= RECEIVE =================
    private List<byte> Buffer = new List<byte>();

    private void ReceiveLoop()
    {
        try
        {
            byte[] recvBuffer = new byte[8192];

            while (true)
            {
                int received = ClientSocket.Receive(recvBuffer);
                if (received <= 0)
                {

                    continue;
                }
                // aggiungi nuovi byte al buffer
                Buffer.AddRange(new ArraySegment<byte>(recvBuffer, 0, received));

                // processa tutti i pacchetti completi
                ProcessPackets();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("ReceiveLoop error: " + e);
            Disconnect();
        }
    }
    private void ProcessPackets()
    {
        int offset = 0;

        while (true)
        {
            // header incompleto
            if (Buffer.Count - offset < 6)
                break;

            // leggi MsgId
            ushort msgId = (ushort)(Buffer[offset] | (Buffer[offset + 1] << 8));

            // leggi length
            int len = Buffer[offset + 2] |
                      (Buffer[offset + 3] << 8) |
                      (Buffer[offset + 4] << 16) |
                      (Buffer[offset + 5] << 24);

            int packetSize = 6 + len;

            // pacchetto incompleto
            if (Buffer.Count - offset < packetSize)
                break;

            // estrai payload
            byte[] payload = Buffer.GetRange(offset + 6, len).ToArray();

            // callback
            OnPacketReceived(new NetPacket
            {
                MsgId = (MsgId)msgId,
                Payload = payload
            });

            offset += packetSize;
        }

        // rimuovi byte processati
        if (offset > 0)
            Buffer.RemoveRange(0, offset);
    }


    // ================= CALLBACK =================
    protected virtual void OnPacketReceived(NetPacket packet)
    {
        // override / event
        Console.WriteLine($"Received {packet.MsgId} ({packet.Payload.Length} bytes)");
        if(packet.MsgId == MsgId.ScPlayerLogin)
        {
            ScPlayerLogin rsp1 = ScPlayerLogin.Parser.ParseFrom(packet.Payload);
            Game.world = new();
            Game.world.Start(true);
            Game.Instance.GamePause = false;
        }
        if (packet.MsgId == MsgId.ScAskChunkData)
        {
            ScAskChunkData rsp = ScAskChunkData.Parser.ParseFrom(packet.Payload);
            Chunk c=Game.world.GetChunkAt(rsp.X, rsp.Z);
            if (c!=null)
            {
                c.SetData(rsp);
            }
        }
    }

    public void Disconnect()
    {
        ClientSocket?.Close();
    }
}

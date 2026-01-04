using Google.Protobuf;
using StardustCraft.Protocol;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace StardustCraft.Server
{


    public class ClientConnection
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        private readonly byte[] _buffer = new byte[8192];
        private int _count;

        public ClientConnection(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
        }

        public async Task RunAsync(
            Action<ClientConnection, NetPacket> onPacket,
            Action onDisconnect)
        {
            try
            {
                while (true)
                {
                    int read = await _stream.ReadAsync(
                        _buffer,
                        _count,
                        _buffer.Length - _count
                    );

                    if (read <= 0)
                        break;

                    _count += read;
                    ProcessBuffer(onPacket);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[SERVER] Client error: " + e.Message);
            }
            finally
            {
                _client.Close();
                onDisconnect?.Invoke();
            }
        }

        private void ProcessBuffer(Action<ClientConnection, NetPacket> onPacket)
        {
            int offset = 0;

            while (true)
            {
                if (_count - offset < 6)
                    break;

                ushort msgId = (ushort)(_buffer[offset] | (_buffer[offset + 1] << 8));
                int len =
                    _buffer[offset + 2] |
                    (_buffer[offset + 3] << 8) |
                    (_buffer[offset + 4] << 16) |
                    (_buffer[offset + 5] << 24);

                int size = 6 + len;
                if (_count - offset < size)
                    break;

                var payload = new byte[len];
                Buffer.BlockCopy(_buffer, offset + 6, payload, 0, len);

                onPacket(this, new NetPacket
                {
                    MsgId = (MsgId)msgId,
                    Payload = payload
                });

                offset += size;
            }

            if (offset > 0)
            {
                Buffer.BlockCopy(_buffer, offset, _buffer, 0, _count - offset);
                _count -= offset;
            }
        }

        public async Task SendAsync(NetPacket packet)
        {
            var data = packet.ToBytes();
            await _stream.WriteAsync(data, 0, data.Length);
        }
        public async Task SendAsync(MsgId id, IMessage data)
        {
            await SendAsync(new NetPacket() { MsgId = id, Payload = data.ToByteArray() });
        }
    }

}

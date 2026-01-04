using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.Protocol
{
    public class NetPacket
    {
        public MsgId MsgId;
        public byte[] Payload;

        public byte[] ToBytes()
        {
            var buffer = new byte[6 + Payload.Length];

            // MsgId (ushort, little-endian)
            ushort id = (ushort)MsgId;
            buffer[0] = (byte)id;
            buffer[1] = (byte)(id >> 8);

            // Length (int, little-endian)
            int len = Payload.Length;
            buffer[2] = (byte)len;
            buffer[3] = (byte)(len >> 8);
            buffer[4] = (byte)(len >> 16);
            buffer[5] = (byte)(len >> 24);

            Buffer.BlockCopy(Payload, 0, buffer, 6, len);
            return buffer;
        }

        public static NetPacket FromBytes(byte[] data)
        {
            if (data.Length < 6)
                throw new Exception("Packet too small");

            ushort msgId = (ushort)(data[0] | (data[1] << 8));
            int len = data[2] | (data[3] << 8) | (data[4] << 16) | (data[5] << 24);

            if (data.Length < 6 + len)
                throw new Exception("Invalid payload length");

            var payload = new byte[len];
            Buffer.BlockCopy(data, 6, payload, 0, len);

            return new NetPacket
            {
                MsgId = (MsgId)msgId,
                Payload = payload
            };
        }
    }
}

using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.Server
{
    public class World
    {
        public List<Chunk> chunks = new();


        public World() { }

        public Chunk GetChunk(int x, int z)
        {
            Chunk c = chunks.Find(c=>c.x == x && c.z == z);
            if (c == null)
            {
                c = new(x, z);
                chunks.Add(c);
            }
            return c;
        }
    }
    public enum BlockType
    {
        Air,
        Grass,
        Dirt,
        Stone,
        SnowGrass,
        OakLeaves,
        OakLog,
        Rose,
        Dandelion,
        Water,
        Gravel,
        Sand,
        Podzol,
        IronOre,
        CoalOre,
        Bedrock,
        OakPlanks
    }
    public class Chunk
    {
        public int x;
        public int z;
        public byte[] chunkData;

        public Chunk(int x, int z)
        {
            this.x = x;
            this.z = z;
            chunkData = new byte[16 * 256 * 16];

            GenerateFlat();
        }

        public ScAskChunkData ToProto()
        {
            return new ScAskChunkData()
            {
                X = x,
                Z = z,
                Blocks = ByteString.CopyFrom(chunkData)
            };
        }
        private void GenerateFlat()
        {
            for (int y = 0; y < 256; y++)
            {
                byte block;

                if (y == 0)
                    block = (byte)BlockType.Bedrock;
                else if (y < 63)
                    block = (byte)BlockType.Stone;
                else if (y == 63)
                    block = (byte)BlockType.Grass;
                else
                    block = (byte)BlockType.Air;

                for (int z = 0; z < 16; z++)
                    for (int x = 0; x < 16; x++)
                    {
                        int index = x + 16 * (z + 16 * y);
                        chunkData[index] = block;
                    }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardustCraft.World.ChunkRenderer;
using static System.Reflection.Metadata.BlobBuilder;

namespace StardustCraft.World.Blocks
{
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

    public class Block
    {
        public BlockType Type;
        public bool IsSolid;
        public bool IsTransparent;
        public byte LightEmission = 0;
        public Block()
        {
            Type = BlockType.Air;
            IsTransparent = true;
        }
        public void RegisterTextures(BlockTextureData data)
        {
            ChunkRenderer.RegisterBlockTextures(Type, data);
        }
    }

}

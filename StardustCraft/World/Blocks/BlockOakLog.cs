using StardustCraft.World.Blocks;


namespace StardustCraft.World
{
   
    public class BlockOakLog : Block
    {
        public BlockOakLog()
        {
            this.Type = BlockType.OakLog;
            this.IsTransparent = false;
            this.IsSolid = true;

            RegisterTextures(new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/oak_log_top.png",
                BottomTexturePath = "Blocks/oak_log_top.png",
                NorthTexturePath = "Blocks/oak_log.png",
                SouthTexturePath = "Blocks/oak_log.png",
                WestTexturePath = "Blocks/oak_log.png",
                EastTexturePath = "Blocks/oak_log.png"
            });
        }
    }
    
}

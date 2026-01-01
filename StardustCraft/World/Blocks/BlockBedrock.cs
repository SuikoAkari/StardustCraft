using StardustCraft.World.Blocks;


namespace StardustCraft.World
{
   
    public class BlockBedrock : Block
    {
        public BlockBedrock()
        {
            this.Type = BlockType.Bedrock;
            this.IsTransparent = false;
            this.IsSolid = true;

            RegisterTextures(new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/stone2.png",
                BottomTexturePath = "Blocks/stone2.png",
                NorthTexturePath = "Blocks/stone2.png",
                SouthTexturePath = "Blocks/stone2.png",
                WestTexturePath = "Blocks/stone2.png",
                EastTexturePath = "Blocks/stone2.png"
            });
        }
    }
    
}

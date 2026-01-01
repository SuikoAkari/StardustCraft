using StardustCraft.World.Blocks;


namespace StardustCraft.World
{
   
    public class BlockSand : Block
    {
        public BlockSand()
        {
            this.Type = BlockType.Sand;
            this.IsTransparent = false;
            this.IsSolid = true;

            RegisterTextures(new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/sand.png",
                BottomTexturePath = "Blocks/sand.png",
                NorthTexturePath = "Blocks/sand.png",
                SouthTexturePath = "Blocks/sand.png",
                WestTexturePath = "Blocks/sand.png",
                EastTexturePath = "Blocks/sand.png",
            });
        }
    }
    
}

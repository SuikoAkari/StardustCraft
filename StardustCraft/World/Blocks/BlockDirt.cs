using StardustCraft.World.Blocks;


namespace StardustCraft.World
{
   
    public class BlockDirt : Block
    {
        public BlockDirt()
        {
            this.Type = BlockType.Dirt;
            this.IsTransparent = false;
            this.IsSolid = true;

            RegisterTextures(new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/Dirt/dirt.png",
                BottomTexturePath = "Blocks/Dirt/dirt.png",
                NorthTexturePath = "Blocks/Dirt/dirt.png",
                SouthTexturePath = "Blocks/Dirt/dirt.png",
                WestTexturePath = "Blocks/Dirt/dirt.png",
                EastTexturePath = "Blocks/Dirt/dirt.png"
            });
        }
    }
    
}

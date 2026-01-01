using StardustCraft.World.Blocks;


namespace StardustCraft.World
{
   
    public class BlockSnowGrass : Block
    {
        public BlockSnowGrass()
        {
            this.Type = BlockType.SnowGrass;
            this.IsTransparent = false;
            this.IsSolid = true;

            RegisterTextures(new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/Snow/snow.png",
                BottomTexturePath = "Blocks/Dirt/dirt.png",
                NorthTexturePath = "Blocks/Snow/grass_block_snow.png",
                SouthTexturePath = "Blocks/Snow/grass_block_snow.png",
                WestTexturePath = "Blocks/Snow/grass_block_snow.png",
                EastTexturePath = "Blocks/Snow/grass_block_snow.png"
            });
        }
    }
    
}

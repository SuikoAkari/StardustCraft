using StardustCraft.World.Blocks;


namespace StardustCraft.World
{
   
    public class BlockGrass : Block
    {
        public BlockGrass()
        {
            this.Type = BlockType.Grass;
            this.IsTransparent = false;
            this.IsSolid = true;

            RegisterTextures(new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/Grass/grass_top.png",
                BottomTexturePath = "Blocks/Dirt/dirt.png",
                NorthTexturePath = "Blocks/Grass/grass_side.png",
                SouthTexturePath = "Blocks/Grass/grass_side.png",
                WestTexturePath = "Blocks/Grass/grass_side.png",
                EastTexturePath = "Blocks/Grass/grass_side.png"
            });
        }
    }
    
}

using StardustCraft.World.Blocks;


namespace StardustCraft.World
{
   
    public class BlockOakPlanks : Block
    {
        public BlockOakPlanks()
        {
            this.Type = BlockType.OakPlanks;
            this.IsTransparent = false;
            this.IsSolid = true;

            RegisterTextures(new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/oak_planks.png",
                BottomTexturePath = "Blocks/oak_planks.png",
                NorthTexturePath = "Blocks/oak_planks.png",
                SouthTexturePath = "Blocks/oak_planks.png",
                WestTexturePath = "Blocks/oak_planks.png",
                EastTexturePath = "Blocks/oak_planks.png"
            });
        }
    }
    
}

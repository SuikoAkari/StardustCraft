using StardustCraft.World.Blocks;


namespace StardustCraft.World
{
   
    public class BlockStone : Block
    {
        public BlockStone()
        {
            this.Type = BlockType.Stone;
            this.IsTransparent = false;
            this.IsSolid = true;

            RegisterTextures(new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/Stone/stone.png",
                BottomTexturePath = "Blocks/Stone/stone.png",
                NorthTexturePath = "Blocks/Stone/stone.png",
                SouthTexturePath = "Blocks/Stone/stone.png",
                WestTexturePath = "Blocks/Stone/stone.png",
                EastTexturePath = "Blocks/Stone/stone.png"
            });
        }
    }
    
}

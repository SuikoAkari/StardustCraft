using StardustCraft.World.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.World
{
    // Classe per gestire l'inizializzazione dei blocchi
    public static class BlockManager
    {
        public static Dictionary<BlockType, Block> Blocks = new();

        public static void RegisterNewBlock(Block block)
        {
            if (block != null)
            {
                Blocks.Add(block.Type, block);
            }
        }
        public static Block GetBlock(BlockType type)
        {
            if (Blocks.ContainsKey(type))
            {
                return Blocks[type];
            }
            else
            {
                return Blocks[BlockType.Air];
            }
            
        }
        public static void Initialize()
        {
            RegisterNewBlock(new Block()); //AIR
            RegisterNewBlock(new BlockStone()); //STONE
            RegisterNewBlock(new BlockGrass()); //GRASS
            RegisterNewBlock(new BlockDirt()); //DIRT
            RegisterNewBlock(new BlockSnowGrass()); //SNOW GRASS
            RegisterNewBlock(new BlockBedrock()); //BEDROCK
            RegisterNewBlock(new BlockCoalOre());
            RegisterNewBlock(new BlockIronOre());
            RegisterNewBlock(new BlockSand());
            RegisterNewBlock(new BlockOakPlanks());
            var podzolTextures = new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/podzol_top.png",
                BottomTexturePath = "Blocks/Dirt/dirt.png",
                NorthTexturePath = "Blocks/podzol_side.png",
                SouthTexturePath = "Blocks/podzol_side.png",
                WestTexturePath = "Blocks/podzol_side.png",
                EastTexturePath = "Blocks/podzol_side.png",
            };

            ChunkRenderer.RegisterBlockTextures(BlockType.Podzol, podzolTextures);


            ChunkRenderer.RegisterBlockTextures(BlockType.Gravel, new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/andesite.png",
                BottomTexturePath = "Blocks/andesite.png",
                NorthTexturePath = "Blocks/andesite.png",
                SouthTexturePath = "Blocks/andesite.png",
                WestTexturePath = "Blocks/andesite.png",
                EastTexturePath = "Blocks/andesite.png",
            });
            ChunkRenderer.RegisterBlockTextures(BlockType.Water, new ChunkRenderer.BlockTextureData
            {
                TopTexturePath = "Blocks/water_still.png",
                BottomTexturePath = "Blocks/water_still.png",
                NorthTexturePath = "Blocks/water_still.png",
                SouthTexturePath = "Blocks/water_still.png",
                WestTexturePath = "Blocks/water_still.png",
                EastTexturePath = "Blocks/water_still.png",
            });
        }
    }
}

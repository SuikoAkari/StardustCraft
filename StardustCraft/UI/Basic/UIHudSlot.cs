using StardustCraft.Graphics;
using OpenTK.Mathematics;
using StardustCraft.World.Entities;
using FontStashSharp;
using StardustCraft.World.Blocks;
using StardustCraft.World;

namespace StardustCraft.UI.Basic
{
    public class UIHudSlot : UIElement
    {
        private int tex;
        private string unselectedSlotTexture = "ui/inventory_slot.png";
        private string selectedSlotTexture = "ui/inventory_selected_slot.png";
        private string outlineSlotTexture = "ui/inventory_selected_slot_outline.png";
        public int slot = 0;
        public override void Init()
        {
            base.Init();

        }

        public override void ComputeLayout(Vector2 parentPos, Vector2 parentSize)
        {
            // Applica margin esterno
            Vector2 posWithMargin = parentPos;

            // Calcola posizione container basata su anchor
            base.ComputeLayout(posWithMargin, parentSize);
           
        }
        public override void Update(float dt)
        {
            PlayerEntity owner = Game.world.GetClientEntity();
            if (owner!=null)
            {
                tex = TextureLoader.GetTexture(owner.selectedInventorySlot == slot ? selectedSlotTexture : unselectedSlotTexture);
            }
            
        }
        public override void Render()
        {
            if (tex != null)
                UserInterface.RenderQuad(computedPos, size, Vector4.One, tex);
            
            PlayerEntity owner = Game.world.GetClientEntity();
            if (owner != null)
            {
                BlockType type = owner.inventory[slot];
                int[] textures ={
                    ChunkRenderer.GetTextureId(type, ChunkRenderer.BlockFace.South), // FRONT
                    ChunkRenderer.GetTextureId(type, ChunkRenderer.BlockFace.North), // BACK
                    ChunkRenderer.GetTextureId(type, ChunkRenderer.BlockFace.West),  // LEFT
                    ChunkRenderer.GetTextureId(type, ChunkRenderer.BlockFace.East),  // RIGHT
                    ChunkRenderer.GetTextureId(type, ChunkRenderer.BlockFace.Top),   // TOP
                    ChunkRenderer.GetTextureId(type, ChunkRenderer.BlockFace.Bottom) // BOTTOM
                };
                Vector3 guiRotation = new Vector3(30f, 45, 180f);
                Game.Instance.cubeRenderer.RenderUI(screenPos: computedPos+size/2, size: 35, rotation: guiRotation, textures: textures, screenSize: UserInterface.Size);
                if (owner.selectedInventorySlot == slot)
                    UserInterface.RenderQuad(computedPos - new Vector2(3, 3), size+new Vector2(6,6), Vector4.One, TextureLoader.GetTexture(outlineSlotTexture));
            }

            UserInterface.RenderQuad(computedPos, new Vector2(18,18), Vector4.One, TextureLoader.GetTexture("ui/inventory_slot_keybind.png"));
            var keybind = "" + (slot + 1);
            System.Numerics.Vector2 textSize = UserInterface.MeasureString(16, keybind);
            UserInterface.RenderText(16, keybind, new Vector2((computedPos.X + 18 / 2) - textSize.X / 2, computedPos.Y), FSColor.LightGray);
           
        }
    }
}

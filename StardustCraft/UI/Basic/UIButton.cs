using StardustCraft.Graphics;
using OpenTK.Mathematics;
using StardustCraft.World.Entities;
using FontStashSharp;
using StardustCraft.World.Blocks;
using StardustCraft.World;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace StardustCraft.UI.Basic
{
    public class UIButton : UIElement
    {
        private int tex;
        private string unselectedSlotTexture = "ui/button_normal.png";
        private string selectedSlotTexture = "ui/button_selected.png";
        public bool hovered;
        public string text = "Button";
        public int fontSize = 16;
        public Action Action;
        public override void Init()
        {
            base.Init();

        }
        private bool IsMouseHover()
        {
            Vector2 mouse = Game.Instance.MousePosition;

            return mouse.X >= computedPos.X &&
                   mouse.X <= computedPos.X + size.X &&
                   mouse.Y >= computedPos.Y &&
                   mouse.Y <= computedPos.Y + size.Y;
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
            tex = TextureLoader.GetTexture(hovered ? selectedSlotTexture : unselectedSlotTexture);
            hovered = IsMouseHover();
            if (IsClicked() && Action!=null)
            {
                Action.Invoke();
            }
        }
        public bool IsClicked()
        {
            return hovered && Game.Instance.IsMouseButtonPressed(MouseButton.Left);
        }
        public override void Render()
        {
            if (tex != null)
                UserInterface.RenderQuad(computedPos, size, Vector4.One, tex);
            
            System.Numerics.Vector2 textSize = UserInterface.MeasureString(fontSize, text);
            UserInterface.RenderText(fontSize, text, (computedPos+size/2)-new Vector2(textSize.X,textSize.Y)/2, FSColor.White);
        }
    }
}

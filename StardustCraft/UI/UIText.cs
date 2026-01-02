using FontStashSharp;
using FontStashSharp.RichText;
using OpenTK.Mathematics;

namespace StardustCraft.UI
{
    public class UIText : UIElement
    {
        public string text = "";
        public int fontSize;

        public override void ComputeLayout(Vector2 parentPos, Vector2 parentSize)
        {
            // Calcola posizione base in base all'anchor
            Vector2 anchorPos = LayoutUtil.ResolveAnchor(anchor, parentPos, parentSize, size, UserInterface.Size.Y);

            // Applica pos locale (dal JSON)
            anchorPos.X += pos.X;
            anchorPos.Y += pos.Y;

            // Inverti Y per OpenTK/top-left
            computedPos = new Vector2(anchorPos.X, anchorPos.Y- UserInterface.Size.Y);
        }

        public override void Render()
        {
            UserInterface.RenderText(fontSize, text, new Vector2(computedPos.X, computedPos.Y), FSColor.White);
        }
    }

}

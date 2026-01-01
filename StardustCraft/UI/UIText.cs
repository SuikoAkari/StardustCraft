using FontStashSharp;
using OpenTK.Mathematics;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            computedPos = new Vector2(parentPos.X, UserInterface.Size.Y - parentPos.Y);
        }

        public override void Render()
        {
            var font = UserInterface._fontSystem.GetFont(fontSize);

            // Y invertita per OpenTK: TextSystem usa top-left, quindi convertiamo
            float renderY = UserInterface.Size.Y - computedPos.Y;
            var pos = new System.Numerics.Vector2(computedPos.X, renderY);

            UserInterface.textRenderer.Begin(new Vector2i((int)UserInterface.Size.X, (int)UserInterface.Size.Y));
            font.DrawText(
                UserInterface.textRenderer,
                text,
                pos,
                FSColor.White,
                scale: new System.Numerics.Vector2(0.7f, 0.7f),
                effect: FontSystemEffect.Stroked,
                effectAmount: 4
            );
            UserInterface.textRenderer.End();
        }
    }

}

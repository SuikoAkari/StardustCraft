using StardustCraft.Bundles.BundleSystem;
using StardustCraft.Graphics;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.UI
{
    public class UIContainer : UIElement
    {
        public string texture;
        public int[] margin;
        public int[] padding;
        public string display;

        public List<UIElement> content = new();

        private int tex;

        public override void Init()
        {
            base.Init();

            if (!string.IsNullOrEmpty(texture))
                tex = TextureLoader.GetTexture(texture);

            foreach (var c in content)
                c.Init();
        }

        public override void ComputeLayout(Vector2 parentPos, Vector2 parentSize)
        {
            // Applica margin esterno
            Vector2 posWithMargin = parentPos + new Vector2(margin[0], -margin[1]);
           
            // Calcola posizione container basata su anchor
            base.ComputeLayout(posWithMargin, parentSize);
            if (display == "flex:column")
            {
                // Start dall'alto del container (TopLeft) considerando padding.top
                float cursorY = computedPos.Y + padding[1];
                float cursorX = computedPos.X + padding[0];

                foreach (var c in content)
                {
                    // Posizione del figlio
                    Vector2 childPos = new Vector2(cursorX, cursorY);

                    // Passa size del container per eventuale anchor interno
                    c.ComputeLayout(childPos, size);

                    // Aggiorna cursore (verso il basso)
                    cursorY += c.size.Y;
                }
            }
            else
            {
                // Layout normale: posizioni relative
                foreach (var c in content)
                {
                    c.ComputeLayout(computedPos, size);
                }
            }
        }
        public override void Update(float dt)
        {

            foreach (var c in content)
                c.Update(dt);
        }
        public override void Render()
        {
            if (tex != null)
                UserInterface.RenderQuad(computedPos, size,Vector4.One, tex);

            foreach (var c in content)
                c.Render();
        }
    }
}

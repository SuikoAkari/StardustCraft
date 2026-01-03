using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.UI.Basic
{
    public class UIMainHud : UILayout
    {
        public UIMainHud() {

            int n = 9;
            float slotWidth = 75f;
            float spacing = 4f;
            float totalWidth = n * slotWidth + (n - 1) * spacing;
            name = "GameHUDv2";
            for (int i = 0; i < n; i++)
            {
                content.Add(new UIHudSlot()
                {
                    anchor = UIAnchor.CenterBottom,
                    name = "hotbar_" + i,
                    size = new OpenTK.Mathematics.Vector2(slotWidth, slotWidth),
                    type = "Container",
                    pos = new OpenTK.Mathematics.Vector2(
                        -totalWidth / 2 + i * (slotWidth + spacing) + slotWidth / 2,
                        -24
                    ),
                    slot = i
                });
            }
        }
    }
}

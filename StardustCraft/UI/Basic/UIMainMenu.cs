using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.UI.Basic
{
    public class UIMainMenu : UILayout
    {
        public UIMainMenu() {

            name = "MainMenu";
            content.Add(new UIButton()
            {
                anchor = UIAnchor.Center,
                name = "playBtn",
                size = new Vector2(221, 38),
                type = "Container",
                pos = new Vector2(0, 0),
                text = "Play",
                Action = () =>
                {
                    Game.world = new();
                    Game.world.Start();
                    Game.Instance.GamePause = false;
                }
            });
        }
    }
}

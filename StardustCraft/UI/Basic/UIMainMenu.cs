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
                text = "Play in Singleplayer",
                Action = () =>
                {
                    Game.world = new();
                    Game.world.Start();
                    Game.Instance.GamePause = false;
                }
            });
            content.Add(new UIButton()
            {
                anchor = UIAnchor.Center,
                name = "playBtn",
                size = new Vector2(221, 38),
                type = "Container",
                pos = new Vector2(0, -42),
                text = "Connect to Test server",
                Action = () =>
                {
                    Game.Instance.NetManager = new();
                    _ = Game.Instance.NetManager.ConnectAsync("127.0.0.1", 25565);
                }
            });
        }
    }
}

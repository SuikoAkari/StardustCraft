using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.UI
{
    public class UILayout
    {
        public string name;
        public string luaFile;
        public List<UIElement> content=new();

        public void Init()
        {
            foreach (var e in content)
                e.Init();
        }

        public void Compute(Vector2 screenSize)
        {
            foreach (var e in content)
                e.ComputeLayout(Vector2.Zero, screenSize);
        }

        public void Render()
        {
            foreach (var e in content)
                e.Render();
        }

        public void Update(float dt)
        {
            foreach (var e in content)
                e.Update(dt);
        }
    }

}

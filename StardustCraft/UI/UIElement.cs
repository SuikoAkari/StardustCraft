using StardustCraft.Lua;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.UI
{
    public abstract class UIElement
    {
        public string name;
        public UIAnchor anchor;
        public Vector2 pos;
        public Vector2 size;
        public string type;
        public string luaFile;

        protected Vector2 computedPos;

        protected LuaComponent lua;

        public virtual void Init()
        {
             if (!string.IsNullOrEmpty(luaFile))
                lua = new LuaComponent(luaFile, this);
        }
        public Game GetGameInstance()
        {
            return Game.Instance;
        }
        public virtual void ComputeLayout(Vector2 parentPos, Vector2 parentSize)
        {
            // Calcola posizione base in base all'anchor
            Vector2 anchorPos = LayoutUtil.ResolveAnchor(anchor, parentPos, parentSize, size, UserInterface.Size.Y);

            // Applica pos locale (dal JSON)
            anchorPos.X += pos.X;
            anchorPos.Y -= pos.Y;

            // Inverti Y per OpenTK/top-left
            computedPos = new Vector2(anchorPos.X, UserInterface.Size.Y - anchorPos.Y - size.Y);
        }

        public abstract void Render();

        public virtual void Update(float dt)
        {
            lua?.Call("update", dt);
        }
    }
    public static class LayoutUtil
    {
        public static Vector2 ResolveAnchor(
    UIAnchor anchor,
    Vector2 parentPos,
    Vector2 parentSize,
    Vector2 size,
    float screenHeight)
        {
            return anchor switch
            {
                UIAnchor.Center =>
                    parentPos + parentSize / 2f - size / 2f,

                UIAnchor.LeftTop =>
                    parentPos + new Vector2(0, parentSize.Y - size.Y),

                UIAnchor.LeftBottom =>
                    parentPos,

                UIAnchor.RightTop =>
                    parentPos + new Vector2(parentSize.X - size.X, parentSize.Y - size.Y),

                UIAnchor.RightBottom =>
                    parentPos + new Vector2(parentSize.X - size.X, 0),

                UIAnchor.CenterTop =>
                    parentPos + new Vector2(
                        (parentSize.X - size.X) / 2f,
                        parentSize.Y - size.Y
                    ),

                UIAnchor.CenterBottom =>
                    parentPos + new Vector2(
                        (parentSize.X - size.X) / 2f,
                        0
                    ),

                _ => parentPos
            };
        }
    }
}

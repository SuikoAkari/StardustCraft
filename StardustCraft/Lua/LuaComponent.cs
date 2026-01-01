using StardustCraft.Bundles.BundleSystem;
using StardustCraft.UI;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.Lua
{
    public class LuaComponent
    {
        private Script script;
        private UIElement element;

        public LuaComponent(string file, UIElement el)
        {
            element = el;
            script = new Script();

            // registra le classi che vuoi esporre
            UserData.RegisterType<UIElement>();
            UserData.RegisterType<UIText>();
            UserData.RegisterType<UIContainer>();
            UserData.RegisterType<Game>();

            // esporta l'oggetto verso Lua
            script.Globals["ui"] = UserData.Create(el);

            // esegui lo script
            script.DoString(BundleManager.Instance.GetFileText(file));

            // chiama init() se presente
            Call("init");
        }

        public void Call(string fn, params object[] args)
        {
            var f = script.Globals.Get(fn);
            if (f.Type == DataType.Function)
                script.Call(f, args);
        }
    }
}

using CefSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;


namespace StardustCraft.Browser
{
    public class GameBridge
    {
        private GameBrowser browser;

        public GameBridge(GameBrowser gameBrowser)
        {
            this.browser = gameBrowser;
        }
        public string GetToken()
        {
            return Game.Instance.AccountState.GetToken();
        }
        public void SetData(string data)
        {
            Game.Instance.AccountState.SetData(data);
        }
        public void Logout()
        {
            Game.Instance.AccountState.BasicInfo = null;
            Game.Instance.AccountState.SetToken("");
            Close();
        }
        public bool CheckAccount()
        {
            return Game.Instance.AccountState.CheckAccount();
        }
        public void SetToken(string token)
        {
            Game.Instance.AccountState.SetToken(token);

        }
        public void Close()
        {
            Game.Instance.browser = null;
            browser.browser.Dispose();
            
        }
    }
}

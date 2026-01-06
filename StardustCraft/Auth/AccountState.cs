using Newtonsoft.Json;
using StardustCraft.Browser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static StardustCraft.Auth.RequestManager;

namespace StardustCraft.Auth
{
    public class AccountState
    {
        public ApiBasicInfoResponse BasicInfo;
        private string Token;
        public string internalLoginPageURI = "http://127.0.0.1:40001/";
        public bool checkAccount = false;
        public AccountState() { }

        public void Initialize(string tkn=null)
        {
            if (File.Exists("token.txt"))
            {
                Token = File.ReadAllText("token.txt");
            }
            if (tkn != null)
            {
                Token = tkn;
                
            }
            OpenBrowserLogin();
            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:40001/");
            listener.Start();

            Task.Run(() =>
            {
                while (listener.IsListening)
                {
                    var ctx = listener.GetContext();
                    var path = ctx.Request.Url.AbsolutePath.TrimStart('/');
                    if (string.IsNullOrEmpty(path)) path = "index.html";

                    var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sdk_data", path);
                    if (!File.Exists(filePath)) filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sdk_data", "index.html");

                    var bytes = File.ReadAllBytes(filePath);
                    string type = Path.GetExtension(path) switch
                    {
                        ".html" => "text/html",
                        ".js" => "application/javascript",
                        ".css" => "text/css",
                        ".json" => "application/json",
                        ".svg" => "image/svg+xml",
                        ".woff" => "font/woff",
                        ".woff2" => "font/woff2",
                        _ => "application/octet-stream"
                    };
                    ctx.Response.ContentType = type;
                    ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    ctx.Response.OutputStream.Close();
                }
            });
        }
        public void SetToken(string token)
        {
            Token = token;
            File.WriteAllText("token.txt",token);
        }
        public bool CheckAccount()
        {
            return checkAccount;
        }
        public void SetData(string data)
        {
            Console.WriteLine("[SDK] Login Data: "+data);
            BasicInfo = JsonConvert.DeserializeObject<ApiBasicInfoResponse>(data);
        }
        public void OpenBrowserLogin(bool checkAccount=false)
        {
            this.checkAccount = checkAccount;
            Game.Instance.browser = new GameBrowser(Game.Instance.Size.X, Game.Instance.Size.Y, internalLoginPageURI);
        }
        public string GetToken()
        {
            return Token;
        }
        public async Task<bool> GetData()
        {
            BasicInfo = await SDK.GetAccountByToken(Token);
            if(BasicInfo.retcode > 0)
            {
                return false;
            }
            return true;
        }
    }
}

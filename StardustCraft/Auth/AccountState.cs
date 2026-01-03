using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardustCraft.Auth.RequestManager;

namespace StardustCraft.Auth
{
    public class AccountState
    {
        public ApiBasicInfoResponse BasicInfo;
        private string Token;
        
        public AccountState() { }

        public void Initialize()
        {
            if (File.Exists("token.txt"))
            {
                Token = File.ReadAllText("token.txt");
                GetData();
            }
            
        }

        public async void GetData()
        {
            BasicInfo = await SDK.GetAccountByToken(Token);
            
        }
    }
}

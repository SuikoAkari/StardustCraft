using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardustCraft.Auth.RequestManager;

namespace StardustCraft.Auth;

public static class SDK
{
    public static string PROD_API_LOGIN_URL = "https://ac-api.teamstardust.org";
    public static string LastError = "";
    public static string GetApiLoginUrl()
    {
        return PROD_API_LOGIN_URL;
    }
    public static async Task<ApiBasicInfoResponse> GetAccountByToken(string token)
    {
        ApiBasicInfoResponse rsp= await RequestManager.GetApiResponse<ApiBasicInfoResponse>($"{GetApiLoginUrl()}/api/user/info/v1/basic?token={token}");
        if (rsp != null)
        {
            if (rsp.retcode == 0)
            {
                return rsp;
            }
            else
            {
                LastError = rsp.msg;
            }
        }
        return null;
    }
}

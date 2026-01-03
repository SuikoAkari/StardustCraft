using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StardustCraft.Auth;

public static class RequestManager
{
    private static readonly HttpClient client = new HttpClient();
    public class ApiResponse
    {
        public int retcode;
        public string msg;
    }
    public class ApiRequest
    {

    }
    public class ApiGrantRequest : ApiRequest
    {
        public string appId;
        public string token;
    }
    public class ApiTokenRequest : ApiRequest
    {
        public string email;
        public string password;
    }
    public class ApiTokenResponse : ApiResponse
    {
        public class Data
        {
            public string token;
        }
        public Data data;
    }
    public class ApiBasicInfoResponse : ApiResponse
    {
        public class Data
        {
            public string nickname;
            public string email;
            public int account_id;
        }
        public Data data;
    }


    public static async Task<T> GetApiResponse<T>(string url) where T : ApiResponse
    {
        try
        {
            string json = await client.GetStringAsync(url);
            T response = JsonConvert.DeserializeObject<T>(json);
            return response;
        }
        catch
        {
            return null;
        }
    }
    public static async Task<TResponse> PostApiResponse<TRequest, TResponse>(string url, TRequest requestData) where TRequest : ApiRequest where TResponse : ApiResponse
    {
        try
        {
            string jsonBody = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage responseMessage = await client.PostAsync(url, content);

            if (responseMessage.IsSuccessStatusCode)
            {
                string jsonResponse = await responseMessage.Content.ReadAsStringAsync();
                TResponse response = JsonConvert.DeserializeObject<TResponse>(jsonResponse);
                return response;
            }
            else
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }
}

using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SUNLootChecker
{
    public static class ItemGetter
    {
        private static string BaseUrl = "https://gameinfo.albiononline.com/api/gameinfo/items/";
        public async static Task<string> SearchAoItem(string aoItem)
        {
            RestClient client = new RestClient($"{BaseUrl}{aoItem}/data");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = await client.ExecuteAsync(request);
            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                return data.localizedNames["EN-US"];
            }
            else
            {
                Console.WriteLine($"StatusCode: {response.StatusCode}\nContent: {response.Content}");
                throw new Exception();
            }
        }
    }
}

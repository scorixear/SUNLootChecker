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
        public static string BaseUrl = "https://gameinfo.albiononline.com/api/gameinfo/";
        public async static Task<string> SearchAoItem(string aoItem)
        {
            RestClient client = new RestClient($"{BaseUrl}items/{aoItem}/data");
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = await client.ExecuteAsync(request);
            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                return data.localizedNames["EN-US"];
            }
            else
            {
                Console.WriteLine($"StatusCode: {response.StatusCode}\nContent: {response.Content}\nUrl: {BaseUrl}items/{aoItem}/data");

                return null;
                //throw new Exception($"StatusCode: {response.StatusCode}\nContent: {response.Content}\nUrl: {BaseUrl}items/{aoItem}/data");
            }
        }
    }
}

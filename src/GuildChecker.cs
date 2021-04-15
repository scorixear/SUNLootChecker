using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SUNLootChecker
{
    public class GuildChecker
    {
        public static GuildChecker Instance = new GuildChecker();
        private GuildChecker() 
        {
            guildList = GuildList.Load();   
        }

        private GuildList guildList;
        public bool IsRunning { get; private set; } = true;

        public async Task UpdateMembers(MainWindow window)
        {
            IsRunning = true;
            window.Dispatcher.Invoke(() =>
            {
                window.ResultText.Visibility = System.Windows.Visibility.Visible;
                window.ResultText.Text = "Loading guild members (this will take a while) ... ";
                window.loadingGif.Visibility = System.Windows.Visibility.Visible;
                
            });

            if(guildList.MustBeUpdated())
            {
                List<string> newGuildMembers = new List<string>();
                foreach (string guild in Configuration.instance.Guilds)
                {
                    RestClient client = new RestClient($"{ItemGetter.BaseUrl}guilds/{guild}/members");
                    RestRequest request = new RestRequest(Method.GET);
                    IRestResponse response = await client.ExecuteAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        dynamic data = JsonConvert.DeserializeObject(response.Content);

                        foreach (dynamic player in data)
                        {
                            newGuildMembers.Add((string)player.Name);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"StatusCode: {response.StatusCode}\nContent: {response.Content}\nUrl: {ItemGetter.BaseUrl}guilds/{guild}/members");
                        throw new Exception($"StatusCode: {response.StatusCode}\nContent: {response.Content}\nUrl: {ItemGetter.BaseUrl}guilds/{guild}/members");
                    }
                }
                guildList.GuildMembers = newGuildMembers;
                guildList.LastUpdated = DateTime.Now;
                guildList.Save();
            }
            
            
            window.Dispatcher.Invoke(() =>
            {
                window.ResultText.Visibility = System.Windows.Visibility.Collapsed;
                window.loadingGif.Visibility = System.Windows.Visibility.Collapsed;
            });
            IsRunning = false;
        }

        public async Task<bool> CheckPlayer(string playerName)
        {
            while (IsRunning) { await Task.Delay(10); }
            return guildList.GuildMembers.Contains(playerName);
        }

    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SUNLootChecker
{
    public class GuildList
    {
        public static string FileName = "GuildMembers.json";

        public List<string> GuildMembers { get; set; } = new List<string>();
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;


        public void Save()
        {
            File.WriteAllText(Path.Combine(Configuration.BaseLocation, FileName), JsonConvert.SerializeObject(this));
        }

        public bool MustBeUpdated()
        {
            return DateTime.Now - LastUpdated > TimeSpan.FromDays(Configuration.instance.UpdateCycleInDays);
        }

        public static GuildList Load()
        {
            if(!File.Exists(Path.Combine(Configuration.BaseLocation, FileName)))
            {
                new GuildList().Save();
            }
            return JsonConvert.DeserializeObject<GuildList>(File.ReadAllText(Path.Combine(Configuration.BaseLocation, FileName)));
        }
    }
}

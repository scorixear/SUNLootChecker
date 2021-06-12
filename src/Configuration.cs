using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SUNLootChecker
{

    [Serializable]
    public class Configuration
    {
        public static string BaseLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static Configuration instance = Instantiate();

        private static Configuration Instantiate()
        {
            Configuration returnInstance;
            if (File.Exists(Path.Combine(BaseLocation, "Configuration.json"))) {
                returnInstance = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(Path.Combine(BaseLocation, "Configuration.json")));
            } else
            {
                returnInstance = new Configuration();
            }
            return returnInstance;
        }

        

        private Configuration() { }

        public List<string> Guilds { get; set; }
        public uint UpdateCycleInDays { get; set; } = 2;
        
       

    }

}

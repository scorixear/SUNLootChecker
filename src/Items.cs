using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SUNLootChecker
{
    public class Items
    {
        public static Items instance = Instantiate();

        private static Items Instantiate()
        {
            Items returnInstance;
            if (File.Exists(Path.Combine(Configuration.BaseLocation, "Items.json")))
            {
                returnInstance = JsonConvert.DeserializeObject<Items>(File.ReadAllText(Path.Combine(Configuration.BaseLocation, "Items.json")));
            }
            else
            {
                returnInstance = new Items();
            }
            return returnInstance;
        }

        public IReadOnlyDictionary<string, string> ItemList { get; set; } = new Dictionary<string, string>();

        private void Save()
        {
            File.WriteAllText(Path.Combine(Configuration.BaseLocation, "Items.json"), JsonConvert.SerializeObject(this));
        }

        public async Task<string> GetItem(string searchItem)
        {
            if (ItemList.ContainsKey(searchItem))
            {
                return ItemList[searchItem];
            }
            else
            {
                string item = await ItemGetter.SearchAoItem(searchItem);
                if (item != null)
                {
                    ((Dictionary<string, string>)ItemList).Add(searchItem, item);
                    Save();
                }

                return item;
            }
        }
    }
}

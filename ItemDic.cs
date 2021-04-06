using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SUNLootChecker
{

    [Serializable]
    public class ItemDic
    {
        private static string baseLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static ItemDic instance = Instantiate();

        private static ItemDic Instantiate()
        {
            ItemDic returnInstance;
            if (File.Exists(Path.Combine(baseLocation, "itemDic.json"))) {
                returnInstance = JsonConvert.DeserializeObject<ItemDic>(File.ReadAllText(Path.Combine(baseLocation, "itemDic.json")));
            } else
            {
                returnInstance = new ItemDic();
            }
            return returnInstance;
        }

        private void Save()
        {
            File.WriteAllText(Path.Combine(baseLocation, "itemDic.json"), JsonConvert.SerializeObject(this));
        }

        private ItemDic() { }

        public IReadOnlyDictionary<string, string> ItemList { get; set; } = new Dictionary<string, string>();
        public async Task<string> GetItem(string searchItem)
        {
            if (ItemList.ContainsKey(searchItem))
            {
                return ItemList[searchItem];
            }
            else
            {
                string item = await ItemGetter.SearchAoItem(searchItem);
                ((Dictionary<string, string>)ItemList).Add(searchItem, item);
                Save();
                return item;
            }
        }

    }

}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SUNLootChecker
{
    public static class LootComparer
    {

        private static Dictionary<string, List<(string, int)>> ClearItems(Dictionary<string, List<(string, int)>> missingItems)
        {
            Dictionary<string, List<(string, int)>> tempDictionary = new Dictionary<string, List<(string, int)>>(missingItems);
            foreach (KeyValuePair<string, List<(string, int)>> pair in tempDictionary)
            {
                List<(string, int)> tempItems = new List<(string, int)>(pair.Value);
                foreach ((string, int) item in tempItems)
                {
                    if (CheckItem(item.Item1))
                    {
                        missingItems[pair.Key].Remove(item);
                    }
                }
                if (missingItems[pair.Key].Count == 0)
                {
                    missingItems.Remove(pair.Key);
                }
            }

            return missingItems;
        }

        private static bool CheckItem(string item)
        {
            string[] ignoredItems = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(Path.Combine(Configuration.BaseLocation, "Exclude.json")));

            foreach (string exclude in ignoredItems)
            {
                if (item.Contains(exclude))
                {
                    return true;
                }
            }
            return false;
        }

        public static Dictionary<string, List<(string, int)>> ParseChestLog(string text, MainWindow mainWindow)
        {
            Dictionary<string, List<(string, int)>> returnDictionary = new Dictionary<string, List<(string, int)>>();
            string[] lines = text.Split('\n');
            Regex regex = new Regex("^\"\\d\\d\\/\\d\\d\\/\\d\\d\\d\\d \\d\\d:\\d\\d:\\d\\d\" \"([^\"]+)\" \"([^\"]+'s)? ?([^\"]+)\" \"([^\"]+)\" \"\\d+\" \"(-?\\d+)\"\r?$");

            foreach (string line in lines)
            {
                mainWindow.Dispatcher.Invoke(() => mainWindow.TotalProgress.Value += 1);
                if (line == "\"Date\" \"Player\" \"Item\" \"Enchantment\" \"Quality\" \"Amount\"\r" || line.Trim().Length == 0)
                {
                    continue;
                }
                Match match = regex.Match(line);
                if (match.Success)
                {
                    string playerName = match.Groups[1].Value;
                    string itemName = match.Groups[3].Value;
                    int amount = int.Parse(match.Groups[5].Value);
                    if (amount < 0)
                    {
                        continue;
                    }
                    int tier = 0;
                    if (match.Groups[2].Success)
                    {
                        switch (match.Groups[2].Value)
                        {
                            case "Elder's":
                                tier = 8;
                                break;
                            case "Grandmaster's":
                                tier = 7;
                                break;
                            case "Master's":
                                tier = 6;
                                break;
                            case "Expert's":
                                tier = 5;
                                break;
                            case "Adept's":
                                tier = 4;
                                break;
                            case "Journeyman's":
                                tier = 3;
                                break;
                            case "Novice's":
                                tier = 2;
                                break;
                            case "Beginner's":
                                tier = 1;
                                break;
                            default:
                                tier = 0;
                                break;
                        }
                    }

                    if (tier == 0)
                    {
                        tier = 8;
                    }
                    int enchantment = int.Parse(match.Groups[4].Value);
                    if (!returnDictionary.ContainsKey(playerName))
                    {
                        returnDictionary.Add(playerName, new List<(string, int)>());
                    }

                    string output = $"{itemName} | T{tier}.{enchantment}";
                    (string, int) foundItem = returnDictionary[playerName].Find(element => element.Item1 == output);
                    if (foundItem != default)
                    {
                        returnDictionary[playerName].Remove(foundItem);
                        amount += foundItem.Item2;
                    }

                    returnDictionary[playerName].Add((output, amount));
                }
                else
                {
                    return null;
                }
            }

            return returnDictionary;
        }

        public static async Task<Dictionary<string, List<(string, int)>>> ParseAOLootLogger(string text, MainWindow mainWindow)
        {
            Dictionary<string, List<(string, int)>> returnDictionary = new Dictionary<string, List<(string, int)>>();
            string[] lines = text.Split('\n');
            Regex regex = new Regex("^[\\d\\.\\-\\/ :APM]+;([^\"]+);([^\"@]+)(?:@(\\d+))?;(\\d+);@?[^\"]+\r?$");


            foreach (string line in lines)
            {
                mainWindow.Dispatcher.Invoke(() => mainWindow.TotalProgress.Value += 1);
                if (line.Trim().Length == 0)
                {
                    continue;
                }
                Match match = regex.Match(line);
                if (match.Success)
                {
                    string playerName = match.Groups[1].Value;
                    if (!await GuildChecker.Instance.CheckPlayer(playerName))
                    {
                        continue;
                    }
                    string itemName = match.Groups[2].Value;
                    int enchantment = 0;
                    if (match.Groups[3].Success)
                    {
                        enchantment = int.Parse(match.Groups[3].Value);
                    }
                    int amount = int.Parse(match.Groups[4].Value);
                    int tier = 0;
                    if (Regex.IsMatch(itemName, "^T(\\d)_\\w+$"))
                    {
                        try
                        {
                            tier = int.Parse(itemName[1] + "");
                        }
                        catch
                        {
                            Console.WriteLine(itemName);
                        }

                    }

                    if (tier == 0)
                    {
                        continue;
                    }

                    if (!returnDictionary.ContainsKey(playerName))
                    {
                        returnDictionary.Add(playerName, new List<(string, int)>());
                    }
                    string name = await Items.instance.GetItem(itemName);
                    if (name == null)
                    {
                        continue;
                    }
                    Match cutMatch = Regex.Match(name, "(\\w+'s) ([\\w ]+)");
                    if (cutMatch.Success)
                    {
                        name = cutMatch.Groups[2].Value;
                    }

                    string output = $"{name} | T{tier}.{enchantment}";
                    (string, int) foundItem = returnDictionary[playerName].Find(element => element.Item1 == output);
                    if (foundItem != default)
                    {
                        returnDictionary[playerName].Remove(foundItem);
                        amount += foundItem.Item2;
                    }

                    returnDictionary[playerName].Add((output, amount));
                }
                else
                {
                    return null;
                }
            }
            return returnDictionary;

        }

        public static async Task<Dictionary<string, List<(string, int)>>> ParsePlayerLoot(List<Player> playerLog, MainWindow mainWindow)
        {
            Dictionary<string, List<(string, int)>> returnDictionary = new Dictionary<string, List<(string, int)>>();

            foreach (Player player in playerLog)
            {

                // Check if Player is in Guild
                if (!await GuildChecker.Instance.CheckPlayer(player.Name))
                {
                    mainWindow.Dispatcher.Invoke(() => mainWindow.TotalProgress.Value += player.Loots.Count);
                    continue;
                }

                foreach (Loot loot in player.Loots)
                {
                    // Update Progress
                    mainWindow.Dispatcher.Invoke(() => mainWindow.TotalProgress.Value += 1);
                    // Parse Item Name
                    Regex itemRegex = new Regex("([^\"@]+)(?:@(\\d+))?");
                    Match itemMatch = itemRegex.Match(loot.ItemName);
                    if (itemMatch.Success)
                    {
                        // extract name
                        string itemName = itemMatch.Groups[1].Value;
                        int enchantment = 0;
                        // extract enchantment
                        if (itemMatch.Groups[2].Success)
                        {
                            enchantment = int.Parse(itemMatch.Groups[2].Value);
                        }

                        // extract Tier
                        int tier = 0;
                        if (Regex.IsMatch(itemName, "^T(\\d)_\\w+$"))
                        {
                            try
                            {
                                tier = int.Parse(itemName[1] + "");
                            }
                            catch
                            {
                                Console.WriteLine(itemName);
                            }

                        }

                        // if no tier existent, item does not need to be donated
                        if (tier == 0)
                        {
                            continue;
                        }
                        // add player name to dic
                        if (!returnDictionary.ContainsKey(player.Name))
                        {
                            returnDictionary.Add(player.Name, new List<(string, int)>());
                        }
                        // Get Item from chestlog
                        string name = await Items.instance.GetItem(itemName);
                        if (name == null)
                        {
                            continue;
                        }
                        // transform to chestlog format
                        Match cutMatch = Regex.Match(name, "(\\w+'s) ([\\w ]+)");
                        if (cutMatch.Success)
                        {
                            name = cutMatch.Groups[2].Value;
                        }
                        // sum up already added items that are the same.
                        string output = $"{name} | T{tier}.{enchantment}";
                        (string, int) foundItem = returnDictionary[player.Name].Find(element => element.Item1 == output);
                        if (foundItem != default)
                        {
                            returnDictionary[player.Name].Remove(foundItem);
                            loot.Quantity += foundItem.Item2;
                        }
                        returnDictionary[player.Name].Add((output, loot.Quantity));
                    }
                    else
                    {
                        return null;
                    }
                }

            }
            return returnDictionary;
        }

        public static Dictionary<string, List<(string name, int amount)>> CompareLoot(Dictionary<string, List<(string, int)>> playerLoot, Dictionary<string, List<(string, int)>> chestLog, MainWindow mainWindow)
        {
            Dictionary<string, List<(string name, int amount)>> missingItems = new Dictionary<string, List<(string, int)>>();

            foreach (KeyValuePair<string, List<(string, int)>> entry in playerLoot)
            {
                mainWindow.Dispatcher.Invoke(() => mainWindow.TotalProgress.Value += 1);
                if (chestLog.ContainsKey(entry.Key))
                {
                    List<(string name, int amount)> chestEntries = chestLog[entry.Key];
                    List<(string name, int amount)> entries = new List<(string, int)>();
                    foreach ((string name, int amount) item in entry.Value)
                    {
                        (string name, int amount) chestEntry = chestEntries.Find(e => e.name == item.name);
                        if (chestEntry == default)
                        {
                            entries.Add((item.name, item.amount));
                        }
                        else if (item.amount - chestEntry.amount > 0)
                        {
                            entries.Add((item.name, item.amount - chestEntry.amount));
                        }
                    }
                    if (entries.Count > 0)
                    {
                        missingItems.Add(entry.Key, entries);
                    }
                }
                else
                {
                    missingItems.Add(entry.Key, new List<(string name, int amount)>(entry.Value));
                }
            }
            return ClearItems(missingItems);
        }
    }
}

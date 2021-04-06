using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SUNLootChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<ResultEntry> ResultList
        {
            get;
            set;
        } = new ObservableCollection<ResultEntry>();


        public MainWindow()
        {
            InitializeComponent();
        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            CheckButton.Background = new SolidColorBrush(Color.FromRgb(179, 234, 147));
            string playerLootString = AOLootText.Text;
            string chestLootString = ChestLogText.Text;
            Dispatcher.Invoke(() => TotalProgress.Value = 0);
            TotalProgress.Maximum = playerLootString.Split("\n").Length * 2 + chestLootString.Split("\n").Length  + 10;
            ResultList.Clear();
            ResultGrid.ItemsSource = null;
            ResultGrid.ItemsSource = ResultList;
            ResultText.Visibility = Visibility.Collapsed;
            Task t = new Task(async () =>
            {
                Dictionary<string, List<(string, int)>> playerLoot = await ParsePlayerLoot(playerLootString);
                Dictionary<string, List<(string, int)>> chestLog = ParseChestLog(chestLootString);
                Dispatcher.Invoke(() =>
                {
                    ResultText.Text = "";
                });
                if (playerLoot == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ResultText.Text += "Could not parse Player Log. Contact pauluap#3338";
                    });
                }
                if (chestLog == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ResultText.Text += "\nCould not parse Chest Log. Contact pauluap#3338";
                    });
                }

                if (playerLoot == null || chestLog == null)
                {
                    Dispatcher.Invoke(() => ResultText.Visibility = Visibility.Visible);
                    return;
                }


                Dictionary<string, List<(string, int)>> missingItems = new Dictionary<string, List<(string, int)>>();

                foreach (KeyValuePair<string, List<(string, int)>> entry in playerLoot)
                {
                    Dispatcher.Invoke(() => TotalProgress.Value += 1);
                    if (chestLog.ContainsKey(entry.Key))
                    {
                        List<(string, int)> chestEntries = chestLog[entry.Key];
                        List<(string, int)> entries = new List<(string, int)>();
                        foreach ((string, int) item in entry.Value)
                        {
                            (string, int) chestEntry = chestEntries.Find(e => e.Item1 == item.Item1);
                            if (chestEntry == default)
                            {
                                entries.Add((item.Item1, item.Item2));
                            }
                            else if (item.Item2 - chestEntry.Item2 > 0)
                            {
                                entries.Add((item.Item1, item.Item2 - chestEntry.Item2));
                            }
                        }
                        if (entries.Count > 0)
                        {
                            missingItems.Add(entry.Key, entries);
                        }
                    }
                    else
                    {
                        missingItems.Add(entry.Key, new List<(string, int)>(entry.Value));
                    }
                }
                missingItems = ClearItems(missingItems);



                

                foreach (KeyValuePair<string, List<(string, int)>> pair in missingItems)
                {
                    ResultEntry entry = new ResultEntry() { PlayerName = pair.Key, Amount = pair.Value.Count };
                    foreach ((string, int) item in pair.Value)
                    {
                        entry.Items.Add(item.Item1 + " | " + item.Item2);
                    }
                    Dispatcher.Invoke(() => ResultList.Add(entry));
                }
                Dispatcher.Invoke(() =>
                {
                    TotalProgress.Value = TotalProgress.Maximum;
                });

            });
            t.Start();
            await t;
            CheckButton.Background = new SolidColorBrush(Color.FromRgb(221, 221, 221));
            ResultGrid.ItemsSource = null;
            ResultGrid.ItemsSource = ResultList;

        }

        private Dictionary<string, List<(string, int)>> ClearItems(Dictionary<string, List<(string, int)>> missingItems)
        {
            Dictionary<string, List<(string, int)>> tempDictionary = new Dictionary<string, List<(string, int)>>(missingItems);
            foreach(KeyValuePair<string, List<(string, int)>> pair in tempDictionary)
            {
                List<(string, int)> tempItems = new List<(string, int)>(pair.Value);
                foreach((string, int) item in tempItems)
                {
                    if(CheckItem(item.Item1))
                    {
                        missingItems[pair.Key].Remove(item);
                    }
                }
                if(missingItems[pair.Key].Count == 0)
                {
                    missingItems.Remove(pair.Key);
                }
            }

            return missingItems;
        }

        private bool CheckItem(string item)
        {
            string[] ignoredItems = new[] 
            { 
                "Trash", 
                "Bag",
                "Potion",
                "Omelette",
                "Swiftclaw",
                "Soul",
                "Relic",
                "Cape",
                "Stew",
                "Tome of Insight",
                "Horse",
            };

            foreach(string exclude in ignoredItems)
            {
                if(item.Contains(exclude))
                {
                    return true;
                }
            }
            return false;
        }

        private Dictionary<string, List<(string, int)>> ParseChestLog(string text)
        {
            Dictionary<string, List<(string, int)>> returnDictionary = new Dictionary<string, List<(string, int)>>();
            string[] lines = text.Split('\n');
            Regex regex = new Regex("\"\\d\\d\\/\\d\\d\\/\\d\\d\\d\\d \\d\\d:\\d\\d:\\d\\d\" \"(\\w+)\" \"(\\w+'s)? ?([\\w ]+)\" \"(\\d+)\" \"\\d+\" \"(-?\\d+)\"\r?");

            foreach (string line in lines)
            {
                Dispatcher.Invoke(() => TotalProgress.Value += 1);
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
                    if(amount < 0)
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

                    if(tier == 0)
                    {
                        continue;
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

        private async Task<Dictionary<string, List<(string, int)>>> ParsePlayerLoot(string text)
        {
            Dictionary<string, List<(string, int)>> returnDictionary = new Dictionary<string, List<(string, int)>>();
            string[] lines = text.Split('\n');
            Regex regex = new Regex("\\d\\d\\d\\d-\\d\\d-\\d\\d \\d\\d:\\d\\d:\\d\\d [AP]M;(\\w+);(\\w+)(?:@(\\d+))?;(\\d+);@?\\w+");


            foreach (string line in lines)
            {
                Dispatcher.Invoke(() => TotalProgress.Value += 1);
                if (line.Trim().Length == 0)
                {
                    continue;
                }
                Match match = regex.Match(line);
                if (match.Success)
                {
                    string playerName = match.Groups[1].Value;
                    string itemName = match.Groups[2].Value;
                    int enchantment = 0;
                    if (match.Groups[3].Success)
                    {
                        enchantment = int.Parse(match.Groups[3].Value);
                    }
                    int amount = int.Parse(match.Groups[4].Value);
                    int tier = 0;
                    if (Regex.IsMatch(itemName, "T(\\d)_\\w+"))
                    {
                        tier = int.Parse(itemName[1] + "");
                    }

                    if (tier == 0)
                    {
                        continue;
                    }

                    if (!returnDictionary.ContainsKey(playerName))
                    {
                        returnDictionary.Add(playerName, new List<(string, int)>());
                    }
                    string name = await ItemDic.instance.GetItem(itemName);
                    Match cutMatch = Regex.Match(name, "(\\w+'s) ([\\w ]+)");
                    if (cutMatch.Success)
                    {
                        name = cutMatch.Groups[2].Value;
                    }

                    string output = $"{name} | T{tier}.{enchantment}";
                    (string, int) foundItem = returnDictionary[playerName].Find(element => element.Item1 == output);
                    if (foundItem!=default) {
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
    }

    public class ResultEntry
    {
        public string PlayerName { get; set; }
        public int Amount { get; set; }
        public ObservableCollection<string> Items { get; set; } = new ObservableCollection<string>();
    }
}

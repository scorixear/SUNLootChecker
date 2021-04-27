using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
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

        private async void CopyClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (GuildChecker.Instance.IsRunning) return;
            await Task.Run(async () => 
            {

                StringBuilder builder = new StringBuilder();
                List<ResultEntry> clonedResultList = new List<ResultEntry>(ResultList);
                clonedResultList = clonedResultList.OrderByDescending((entry) => entry.Amount).ThenBy((entry) => entry.PlayerName).ToList();
                foreach (ResultEntry entry in clonedResultList)
                {
                    builder.Append($"**{entry.PlayerName}** {entry.Amount}");
                    builder.AppendLine();
                    foreach (string item in entry.Items)
                    {
                        builder.Append("- " + item);
                        builder.AppendLine();
                    }
                    builder.AppendLine();
                }

                await TextCopy.ClipboardService.SetTextAsync(builder.ToString());
            });

        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (GuildChecker.Instance.IsRunning) return;
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
            string[] ignoredItems = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(Path.Combine(Configuration.BaseLocation, "Exclude.json")));

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
            Regex regex = new Regex("^\"\\d\\d\\/\\d\\d\\/\\d\\d\\d\\d \\d\\d:\\d\\d:\\d\\d\" \"([^\"]+)\" \"([^\"]+'s)? ?([^\"]+)\" \"([^\"]+)\" \"\\d+\" \"(-?\\d+)\"\r?$");

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
            Regex regex = new Regex("^[\\d\\.\\-\\/ :APM]+;([^\"]+);([^\"]+)(?:@(\\d+))?;(\\d+);@?[^\"]+\r?$");


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
                        } catch
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
                    string name = await Configuration.instance.GetItem(itemName);
                    if(name == null)
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

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            await GuildChecker.Instance.UpdateMembers(this);
            ResultGrid.Visibility = Visibility.Visible;
            ResultGrid.ItemsSource = null;
        }


        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RefreshMaximizeRestoreButton()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.maximizeButton.Visibility = Visibility.Collapsed;
                this.restoreButton.Visibility = Visibility.Visible;
            }
            else
            {
                this.maximizeButton.Visibility = Visibility.Visible;
                this.restoreButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            this.RefreshMaximizeRestoreButton();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
        }

        public static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                // We need to tell the system what our size should be when maximized. Otherwise it will cover the whole screen,
                // including the task bar.
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

                // Adjust the maximized size and position to fit the work area of the correct monitor
                IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitor != IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                    GetMonitorInfo(monitor, ref monitorInfo);
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                    mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                    mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
                }

                Marshal.StructureToPtr(mmi, lParam, true);
            }

            return IntPtr.Zero;
        }

        private const int WM_GETMINMAXINFO = 0x0024;

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }



    public class ResultEntry
    {
        public string PlayerName { get; set; }
        public int Amount { get; set; }
        public ObservableCollection<string> Items { get; set; } = new ObservableCollection<string>();
    }
}

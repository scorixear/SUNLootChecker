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
            Dispatcher.Invoke(() => TotalProgress.Value = 0);
            if (GuildChecker.Instance.IsRunning) return;
            string playerLootString = AOLootText.Text;
            string chestLootString = ChestLogText.Text;
            List<Player> playerLog = JsonConvert.DeserializeObject<List<Player>>(playerLootString);
           
            TotalProgress.Maximum = playerLog.Sum(player => player.Loots.Count) + chestLootString.Split("\n").Length  + 10;
            ResultList.Clear();
            ResultGrid.ItemsSource = null;
            ResultGrid.ItemsSource = ResultList;
            ResultText.Visibility = Visibility.Collapsed;
            await Task.Run(async () =>
            {
                Dictionary<string, List<(string, int)>> playerLoot = await LootComparer.ParsePlayerLoot(playerLog, this);
                Dictionary<string, List<(string, int)>> chestLog = LootComparer.ParseChestLog(chestLootString, this);
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

                Dictionary<string, List<(string name, int amount)>> missingItems = LootComparer.CompareLoot(playerLoot, chestLog, this);
                foreach (KeyValuePair<string, List<(string name, int amount)>> pair in missingItems)
                {
                    ResultEntry entry = new ResultEntry() { PlayerName = pair.Key, Amount = pair.Value.Sum(item=>item.amount) };
                    foreach ((string name, int amount) item in pair.Value)
                    {
                        entry.Items.Add(item.name + " | " + item.amount);
                    }
                    Dispatcher.Invoke(() => ResultList.Add(entry));
                }
                Dispatcher.Invoke(() =>
                {
                    TotalProgress.Value = TotalProgress.Maximum;
                });

            });
            ResultGrid.ItemsSource = null;
            ResultGrid.ItemsSource = ResultList;
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

        private void GroupBox_Drop(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if(files.Length == 1)
                {
                    string fileName = Path.GetFileName(files[0]);
                    if (fileName.StartsWith("CombatLoots") && fileName.EndsWith(".json"))
                    {
                        AOLootText.Text = File.ReadAllText(files[0]);
                    }
                }
            }
        }

        private void AOLootText_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }



    public class ResultEntry
    {
        public string PlayerName { get; set; }
        public int Amount { get; set; }
        public ObservableCollection<string> Items { get; set; } = new ObservableCollection<string>();
    }
}

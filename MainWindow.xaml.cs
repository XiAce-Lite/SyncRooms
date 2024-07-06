using Microsoft.Toolkit.Uwp.Notifications;
using SyncRooms.Properties;
using SyncRooms.ViewModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using static SyncRooms.FavoriteMembers;

namespace SyncRooms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel MainVM = new();

        private readonly HttpClient? _client;
        private readonly JsonSerializerOptions? _serializerOptions;

        private readonly System.Windows.Threading.DispatcherTimer AutoReloadTimer = new();

        private readonly List<MainWindowViewModel.Member> Alerted = [];

        public MainWindow()
        {
            //前のバージョンのプロパティを引き継ぐぜ。
            Settings.Default.Upgrade();

            InitializeComponent();
            _client = new HttpClient();
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            Closing += MainWindow_Closing;
            ContentRendered += MainWindow_ContentRendered;

            DataContext = MainVM;

            _ = GetDataAsync();

            AutoReloadTimer.Interval = TimeSpan.FromSeconds(Settings.Default.ReloadTiming);
            AutoReloadTimer.Tick += AutoReloadTimer_Tick;
            if (Settings.Default.UseAutoReload)
            {
                AutoReloadTimer.Start();
            }
        }

        private void MainWindow_ContentRendered(object? sender, EventArgs e)
        {
            //Windowロケーションとサイズの復元
            Left = Settings.Default.WindowLocation.X;
            Top = Settings.Default.WindowLocation.Y;
            Width = Settings.Default.WindowSize.Width;
            Height = Settings.Default.WindowSize.Height;

            var fullname = typeof(App).Assembly.Location;
            var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(fullname);
            var ver = info.FileVersion;
            Title = $"SyncRooms:Rooms List V2 ver {ver}";
        }

        private void AutoReloadTimer_Tick(object? sender, EventArgs e)
        {
            _ = GetDataAsync();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            //Windowロケーションとサイズの保管
            Settings.Default.WindowLocation = new System.Drawing.Point((int)Left, (int)Top);
            Settings.Default.WindowSize = new System.Drawing.Size((int)Width, (int)Height);
            Settings.Default.Save();
        }

        private async Task GetDataAsync()
        {
            if (_client is null) { return; }

            string myDoc = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string JsonFile = System.IO.Path.Combine(myDoc, "SyncRooms", "favs.json");

            Uri uri = new("https://webapi.syncroom.appservice.yamaha.com/rooms/guest/online");
            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var json = JsonSerializer.Deserialize<MainWindowViewModel.RoomsRoot>(content, _serializerOptions);
                        if (json != null)
                        {
                            if (json.Rooms != null)
                            {
#nullable disable warnings
                                var ordered = json.Rooms.OrderByDescending(x => x.IsExistFavorite);
                                MainVM.Rooms.Clear();
                                foreach (var item in ordered)
                                {
                                    if (((item.OwnerUser?.IdProvider == "ymid-jp") && (Settings.Default.IsVisibleJp) ||
                                        (item.OwnerUser?.IdProvider == "ymid-kr") && (Settings.Default.IsVisibleKr)) &&
                                        ((item.NeedPasswd == true) && (Settings.Default.IsVisibleLocked) ||
                                        (item.NeedPasswd == false) && (Settings.Default.IsVisibleUnlocked)) ||
                                        (item.IsTestRoom == true)
                                        )
                                    {
                                        MainVM.Rooms.Add(item);
                                    }

                                    var searchAlert = item.Members.Where(el => el.AlertOn == true).ToList();
                                    if (searchAlert.Count > 0)
                                    {
                                        foreach (var alertMember in searchAlert)
                                        {
                                            var exists = Alerted.Where(el => el.UserId == alertMember.UserId).ToList();
                                            if (exists.Count == 0)
                                            {
                                                new ToastContentBuilder()
                                                    .AddText($"{alertMember.Nickname}さん({alertMember.LastPlayedPart.Part})が入室しています。")
                                                    .Show();

                                                Alerted.Add(alertMember);
                                            }
                                        }
                                    }
                                }

                                //ここで退室チェックできるんじゃね？
                                //対象は、AlertOnのJsonから。json.Roomsぶん回してヒットしなければ、退室済み
                                //Alertedの中から削除する。
                                //ファイルがある場合のみ。
                                if (System.IO.Path.Exists(JsonFile))
                                {
                                    //ファイル開く。
                                    var jsonReadData = Tools.GetJsonData(JsonFile);

                                    //中身チェック。あればデシリアライズ
                                    FavRoot? favRoot = Tools.GetFavoriteRoot(jsonReadData);

                                    //既にいるかチェック。
                                    if (favRoot is not null)
                                    {
                                        foreach (var item in favRoot.Members)
                                        {
                                            if (item.AlertOn)
                                            {
                                                if (!content.Contains(item.UserId))
                                                {
                                                    var removeMember = Alerted.Find(el => el.UserId == item.UserId);
                                                    if (removeMember is not null)
                                                    {
                                                        new ToastContentBuilder()
                                                            .AddText($"{removeMember.Nickname}さん({removeMember.LastPlayedPart.Part})が退室しました。")
                                                            .Show();
                                                        Alerted.Remove(removeMember);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
#nullable restore                                   
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }
            return;
        }

        private void GetHTML_Click(object sender, RoutedEventArgs e)
        {
            _ = GetDataAsync();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void UseAutoReload_Click(object sender, RoutedEventArgs e)
        {
            AutoReloadTimer.Interval = TimeSpan.FromSeconds(ReloadTiming.Value);
            if (UseAutoReload.IsChecked == true)
            {
                AutoReloadTimer.Start();
            }
            else
            {
                AutoReloadTimer.Stop();
            }
        }

        private void ReloadTiming_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bool IsRunning = false;

            if (AutoReloadTimer.IsEnabled)
            {
                IsRunning = true;
                AutoReloadTimer.Stop();
            }
            AutoReloadTimer.Interval = TimeSpan.FromSeconds(ReloadTiming.Value);

            if (IsRunning == true)
            {
                AutoReloadTimer.Start();
            }
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            //マイドキュメント
            var temp = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            new ToastContentBuilder()
                .AddText("トーストテスト")
                .AddText($"{temp}")
                .Show();
        }

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            bool IsRunning = false;

            if (AutoReloadTimer.IsEnabled)
            {
                IsRunning = true;
                AutoReloadTimer.Stop();
            }

            _ = GetDataAsync();

            AutoReloadTimer.Interval = TimeSpan.FromSeconds(ReloadTiming.Value);

            if (IsRunning == true)
            {
                AutoReloadTimer.Start();
            }
        }
    }
}
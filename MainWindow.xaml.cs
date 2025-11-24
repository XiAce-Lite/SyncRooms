using Microsoft.Toolkit.Uwp.Notifications;
using SyncRooms.Properties;
using SyncRooms.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using static SyncRooms.FavoriteMembers;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;

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

        private readonly Dictionary<string, MainWindowViewModel.Member> AlertedMembers = [];

        public MainWindow()
        {
            //前のバージョンのプロパティを引き継ぐぜ。
            Settings.Default.Upgrade();

            InitializeComponent();
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(20);
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

            // HttpClientのDispose
            _client?.Dispose();
        }

        private bool IsRoomVisible(MainWindowViewModel.Room item)
        {
            return
                ((item.OwnerUser?.IdProvider == "ymid-jp" && Settings.Default.IsVisibleJp) ||
                 (item.OwnerUser?.IdProvider == "ymid-kr" && Settings.Default.IsVisibleKr)) &&
                ((item.NeedPasswd && Settings.Default.IsVisibleLocked) ||
                 (!item.NeedPasswd && Settings.Default.IsVisibleUnlocked)) ||
                item.IsTestRoom;
        }

        private async Task GetDataAsync()
        {
            if (_client is null) return;

            string myDoc = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string jsonFile = Path.Combine(myDoc, "SyncRooms", "favs.json");

            var roomsRoot = await FetchRoomsAsync();
            if (roomsRoot?.Rooms == null) return;

            var filteredRooms = FilterRooms(roomsRoot.Rooms);

            await UpdateRoomsOnUIAsync(filteredRooms);

            var currentUserIds = GetCurrentUserIds(roomsRoot.Rooms);

            await NotifyRoomAlerts(filteredRooms);
            CheckRoomExits(jsonFile, currentUserIds);
        }

        private async Task<MainWindowViewModel.RoomsRoot?> FetchRoomsAsync()
        {
            Uri uri = new("https://webapi.syncroom.appservice.yamaha.com/rooms/guest/online");
            try
            {
                HttpResponseMessage response = await _client!.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    ShowError($"サーバーエラー: {response.StatusCode}");
                    return null;
                }

                string content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content))
                {
                    ShowError("サーバーからデータが取得できませんでした。");
                    return null;
                }

                return JsonSerializer.Deserialize<MainWindowViewModel.RoomsRoot>(content, _serializerOptions);
            }
            catch (HttpRequestException ex)
            {
                ShowError("ネットワークエラーが発生しました。\n" + ex.Message);
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                ShowError("タイムアウトしました。\n" + ex.Message);
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                ShowError("不明なエラーが発生しました。\n" + ex.Message);
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
                return null;
            }
        }

        private void ShowError(string message)
        {
            // 必要に応じてUIスレッドで実行
            if (Dispatcher.CheckAccess())
            {
                MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Dispatcher.Invoke(() => MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private List<MainWindowViewModel.Room> FilterRooms(List<MainWindowViewModel.Room> rooms)
        {
            return [.. rooms
                .Where(IsRoomVisible)
                .OrderByDescending(x => x.IsExistFavorite)
                .ThenByDescending(x => x.IsExistAlertOn)];
        }

        private async Task UpdateRoomsOnUIAsync(List<MainWindowViewModel.Room> filteredRooms)
        {
            if (Dispatcher.CheckAccess())
            {
                MainVM.Rooms = new ObservableCollection<MainWindowViewModel.Room>(filteredRooms);
            }
            else
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MainVM.Rooms = new ObservableCollection<MainWindowViewModel.Room>(filteredRooms);
                });
            }
        }

        private static HashSet<string> GetCurrentUserIds(List<MainWindowViewModel.Room> rooms)
        {
            return [.. rooms
                .SelectMany(r => r.Members ?? Enumerable.Empty<MainWindowViewModel.Member>())
                .Select(m => m.UserId)];
        }

        private async Task NotifyRoomAlerts(List<MainWindowViewModel.Room> filteredRooms)
        {
            foreach (var room in filteredRooms)
            {
                var members = room.Members ?? Enumerable.Empty<MainWindowViewModel.Member>();
                foreach (var alertMember in members.Where(m => m.AlertOn))
                {
                    if (!AlertedMembers.ContainsKey(alertMember.UserId))
                    {
                        new ToastContentBuilder()
                            .AddText($"{alertMember.Nickname}さん({alertMember.LastPlayedPart?.Part})が「{room.Name}」に入室しています。")
                            .Show();
                        AlertedMembers[alertMember.UserId] = alertMember;

                        // After showing Toast, try to find apikey.dat in user profile and post to Pushbullet if exists.
                        try
                        {
                            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            string apiFile = Path.Combine(userProfile, "apikey.dat");
                            if (!File.Exists(apiFile))
                            {
                                continue;
                            }

                            string? token = null;

                            try
                            {
                                var fileBytes = File.ReadAllBytes(apiFile);
                                try
                                {
                                    // Try to decrypt using DPAPI for current user
                                    var decrypted = ProtectedData.Unprotect(fileBytes, null, DataProtectionScope.CurrentUser);
                                    token = Encoding.UTF8.GetString(decrypted);
                                }
                                catch
                                {
                                    // If decryption fails, try reading as plain text
                                    try
                                    {
                                        token = File.ReadAllText(apiFile);
                                    }
                                    catch
                                    {
                                        token = null;
                                    }
                                }
                            }
                            catch
                            {
                                // ignore and continue
                                token = null;
                            }

                            token = token?.Trim();
                            // remove possible surrounding quotes
                            token = token?.Trim('"');
                            if (string.IsNullOrEmpty(token))
                            {
                                continue;
                            }

                            // Prepare Pushbullet request
                            var pushObj = new
                            {
                                type = "note",
                                title = "Enter Room",
                                body = $"{alertMember.Nickname} enters {room.Name}."
                            };

                            var json = JsonSerializer.Serialize(pushObj, _serializerOptions);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                            // Ensure Content-Type header explicitly set
                            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                            try
                            {
                                // Use PostAsync and await the response to ensure the request is actually sent
                                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.pushbullet.com/v2/pushes");
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                                request.Content = content;

                                var response = await _client!.SendAsync(request);
                                // Dispose response after use
                                response.Dispose();
                            }
                            catch
                            {
                                // Ignore any exceptions from API call and continue
                            }
                        }
                        catch
                        {
                            // Ignore any exception in the whole pushbullet flow and continue
                        }
                    }
                }
            }
        }

        private void CheckRoomExits(string jsonFile, HashSet<string> currentUserIds)
        {
            if (!File.Exists(jsonFile)) return;

            var jsonReadData = Tools.GetJsonData(jsonFile);
            FavRoot? favRoot = Tools.GetFavoriteRoot(jsonReadData);
            if (favRoot == null) return;

            var alertOnMembers = favRoot.Members.Where(m => m.AlertOn).ToList();
            foreach (var member in alertOnMembers)
            {
                if (!currentUserIds.Contains(member.UserId))
                {
                    if (AlertedMembers.TryGetValue(member.UserId, out var removeMember))
                    {
                        new ToastContentBuilder()
                            .AddText($"{removeMember.Nickname}さん({removeMember.LastPlayedPart?.Part})が退室しました。")
                            .Show();
                        AlertedMembers.Remove(removeMember.UserId);
                    }
                }
            }
        }

        private void GetHTML_Click(object sender, RoutedEventArgs e)
        {
            _ = GetDataAsync();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuToggleButton.IsChecked = false;
        }
    }
}
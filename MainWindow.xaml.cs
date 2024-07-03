using SyncRooms.Properties;
using SyncRooms.ViewModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

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

        private System.Windows.Threading.DispatcherTimer AutoReloadTimer = new();

        public MainWindow()
        {
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

            Uri uri = new("https://webapi.syncroom.appservice.yamaha.com/rooms/guest/online");
            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    if (content != null)
                    {
                        var json = JsonSerializer.Deserialize<MainWindowViewModel.RoomsRoot>(content, _serializerOptions);
                        if (json != null)
                        {
                            if (json.Rooms != null)
                            {
#nullable disable warnings

                                MainVM.Rooms.Clear();
                                foreach (var item in json.Rooms)
                                {
                                    MainVM.Rooms.Add(item);
#nullable restore
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }
            return;
        }

        private void GetHTML_Click(object sender, RoutedEventArgs e)
        {
            _= GetDataAsync();
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

            if (AutoReloadTimer.IsEnabled) { 
                IsRunning = true;
                AutoReloadTimer.Stop(); 
            }
            AutoReloadTimer.Interval = TimeSpan.FromSeconds(ReloadTiming.Value);

            if (IsRunning == true) {
                AutoReloadTimer.Start();
            }
        }
    }
}
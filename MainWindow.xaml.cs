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

        public MainWindow()
        {
            InitializeComponent();
            _client = new HttpClient();
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            DataContext = MainVM;

            _ = GetDataAsync();
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

        private async void GetHTML_Click(object sender, RoutedEventArgs e)
        {
            await GetDataAsync();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
using static SyncRooms.FavoriteMembers;

namespace SyncRooms.Controls
{
    /// <summary>
    /// Members.xaml の相互作用ロジック
    /// </summary>
    public partial class Members : UserControl
    {
        private readonly string CurDir = "";
        private readonly string JsonFile = "";
        private static JsonSerializerOptions? _serializerOptions;

        public Members()
        {
            InitializeComponent();

            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true
            };

            CurDir = Directory.GetCurrentDirectory();
            JsonFile = System.IO.Path.Combine(CurDir, "favs.json");
        }

        private static string GetJsonData(string JsonFile)
        {
            using StreamReader sr = File.OpenText(JsonFile);
            var jsonReadData = sr.ReadToEnd();
            sr.Close();
            sr.Dispose();
            return jsonReadData;
        }

        private static FavRoot? GetFavoriteRoot(string jsonReadData)
        {
            FavRoot? root = new();
            //中身チェック。あればデシリアライズ
            if (!string.IsNullOrEmpty(jsonReadData))
            {
                root = JsonSerializer.Deserialize<FavRoot>(jsonReadData);
            }
            return root;
        }

        private void WriteToJsonFile(FavRoot? favRoot)
        {
            if (favRoot == null) { return; }

            //Jsonにシリアライズ。
            string jsonString = JsonSerializer.Serialize<FavRoot>(favRoot, _serializerOptions);

            //追記じゃなく、丸ごと上書き。
            using var sw = new StreamWriter(JsonFile, false, Encoding.UTF8);
            // JSON データをファイルに書き込み
            sw.Write(jsonString);
            sw.Close();
            sw.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="AddOnly">お気に入り追加のみの場合にTrue</param>
        /// <param name="AlertOn">アラートオン時にTrue</param>
        private void AddFavorite(bool AddOnly = true, bool AlertOn = false)
        {
            //ファイルがねぇ場合
            if (!System.IO.Path.Exists(JsonFile))
            {
                //ファイル作る。
                using FileStream fs = File.Create(JsonFile);
                fs.Close();
                fs.Dispose();
            }
            //ファイル開く。
            var jsonReadData = GetJsonData(JsonFile);

            //中身チェック。あればデシリアライズ
            FavRoot? favRoot = GetFavoriteRoot(jsonReadData);

            //既にいるか一応チェック。
            if (favRoot is not null)
            {
                foreach (var item in favRoot.Members)
                {
                    if (item.UserId == UserId.Text) { 
                        if (AddOnly) { return; }
                        favRoot.Members.Remove(item);
                        break;
                    }
                }
            }

            //お気に入りメンバー作る。
            var fav = new Fav
            {
                UserId = UserId.Text,
                Nickname = NickName.Text,
                AlertOn = AlertOn,
            };

            //ルートに追加
            favRoot?.Add(fav);

            //Jsonファイルに書き込み
            WriteToJsonFile(favRoot);

            //お気に入りフラグ立てる。
            IsFavorite.IsChecked = true;

        }

        private void RemoveFav_Click(object sender, RoutedEventArgs e)
        {
            //お気に入りフラグ寝かす。
            IsFavorite.IsChecked = false;

            //ファイルがねぇ場合
            if (!System.IO.Path.Exists(JsonFile)) { return; }

            //ファイル開く。
            var jsonReadData = GetJsonData(JsonFile);

            //中身チェック。あればデシリアライズ
            FavRoot? favRoot = GetFavoriteRoot(jsonReadData);

            //既にいるかチェック。居たら削除。
            if (favRoot is not null)
            {
                foreach (var item in favRoot.Members)
                {
                    if (item.UserId == UserId.Text)
                    {
                        //合致したら削除
                        favRoot.Members.Remove(item);
                        //Jsonファイルに書き込み
                        WriteToJsonFile(favRoot);
                        return;
                    }
                }
            }
        }

        private void AddFav_Click(object sender, RoutedEventArgs e)
        {
            AddFavorite();
        }

        private void AddAlert_Click(object sender, RoutedEventArgs e)
        {
            AddFavorite(false,true);
            AlertOn.IsChecked = true;
        }

        private void RemoveAlert_Click(object sender, RoutedEventArgs e)
        {
            AddFavorite(false, false);
            AlertOn.IsChecked = false;
        }
    }
}

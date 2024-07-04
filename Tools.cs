using System.IO;
using System.Text.Json;
using static SyncRooms.FavoriteMembers;

namespace SyncRooms
{
    class Tools
    {
        public static string GetJsonData(string JsonFile)
        {
            using StreamReader sr = File.OpenText(JsonFile);
            var jsonReadData = sr.ReadToEnd();
            sr.Close();
            sr.Dispose();
            return jsonReadData;
        }

        public static FavRoot? GetFavoriteRoot(string jsonReadData)
        {
            FavRoot? root = new();
            //中身チェック。あればデシリアライズ
            if (!string.IsNullOrEmpty(jsonReadData))
            {
                root = JsonSerializer.Deserialize<FavRoot>(jsonReadData);
            }
            return root;
        }
    }
}

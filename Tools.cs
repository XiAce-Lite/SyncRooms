using System.Diagnostics;
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

        /// <summary>
        /// 指定された文字列を含むウィンドウタイトルを持つプロセスを取得します。
        /// </summary>
        /// <param name="windowTitle">ウィンドウタイトルに含む文字列。</param>
        /// <returns>該当するプロセスの配列。</returns>
        public static Process[] GetProcessesByWindowTitle(string windowTitle)
        {
            System.Collections.ArrayList list = [];

            //すべてのプロセスを列挙する
            foreach (Process p in Process.GetProcesses())
            {
                //指定された文字列がメインウィンドウのタイトルに含まれているか調べる
                if (0 <= p.MainWindowTitle.IndexOf(windowTitle))
                {
                    //含まれていたら、コレクションに追加
                    list.Add(p);
                }
            }

            //コレクションを配列にして返す
            return (Process[])list.ToArray(typeof(Process));
        }

        public static void OpenUrl(string url)
        {
            ProcessStartInfo pi = new()
            {
                FileName = url,
                UseShellExecute = true,
            };

            Process.Start(pi);
        }

        public static async void EnterRoom(string RoomName = "", string RoomId = "", bool NeedPassword = false)
        {
            if (string.IsNullOrEmpty(RoomName) || string.IsNullOrEmpty(RoomId)) { return; }

            string PasswordFlg = "0";
            if (NeedPassword) { PasswordFlg = "1"; }

            string url = $"https://webapi.syncroom.appservice.yamaha.com/launch_app?roomName={RoomName}&roomId={RoomId}&requirePassword={PasswordFlg}";
            await Task.Delay(50);
            OpenUrl(url);
        }
    }
}

using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Automation;
using static SyncRooms.FavoriteMembers;
using static SyncRooms.ViewModel.MainWindowViewModel;

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

        public static async void EnterRoom(string RoomId = "")
        {
            if (string.IsNullOrEmpty(RoomId)) { return; }

            TargetProcess targetProc = new("SYNCROOM2");
            if (targetProc.IsAlive == false)
            {
                return;
            }

            //タイトル検索なので、他のプロセスでも"SYNCROOM"が入ってると…
            Process[] procs = Tools.GetProcessesByWindowTitle("SYNCROOM");
            if (procs.Length == 0)
            {
                return;
            }

            AutomationElement? rootElement = null;
            foreach (Process proc in procs)
            {
                if (proc.MainWindowTitle == "SYNCROOM")
                {
                    //MainWindotTitle が "SYNCROOM"なプロセス＝ターゲットのプロセスは、SYNCROOM2.exeが中で作った別プロセスのようで
                    //こんな面倒なやり方をしてみている。
                    rootElement = AutomationElement.FromHandle(proc.MainWindowHandle);
                    break;
                }
            }

            if (rootElement is null)
            {
                return;
            }

            AutomationElement? webArea = rootElement.FindFirst(TreeScope.Children | TreeScope.Descendants,
                                                                new PropertyCondition(AutomationElement.AutomationIdProperty, "RootWebArea"));
            if (webArea is null)
            {
                return;
            }

            AutomationElement? list = webArea.FindFirst(TreeScope.Element | TreeScope.Descendants,
                                                    new PropertyCondition(AutomationElement.AutomationIdProperty, "list"));

            TreeWalker twCardClass = new(new PropertyCondition(AutomationElement.ClassNameProperty,
                                        "v-card v-theme--base v-card--density-default elevation-0 v-card--variant-elevated room-card"));

            TreeWalker twEdit = new(new PropertyCondition(AutomationElement.LocalizedControlTypeProperty, "編集", PropertyConditionFlags.IgnoreCase));

            AutomationElement vClassCard = twCardClass.GetFirstChild(list);
            if (vClassCard is null) { return; }

            AutomationElement editElement = twEdit.GetFirstChild(vClassCard);
            AutomationElement txtBoxId = twEdit.GetFirstChild(editElement);

            if (txtBoxId is null) { return; }

            TreeWalker twClear = new(new PropertyCondition(AutomationElement.NameProperty, "Clear xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", PropertyConditionFlags.IgnoreCase));
            AutomationElement ClearButton = twClear.GetFirstChild(vClassCard);
            if (ClearButton is not null)
            {
                if (ClearButton.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern clearBtn)
                {
                    clearBtn.Invoke();
                }
            }

            if (txtBoxId.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePattern))
            {
                ((ValuePattern)valuePattern).SetValue(RoomId);
            }

            TreeWalker twButton = new(new PropertyCondition(AutomationElement.NameProperty, "ENTER", PropertyConditionFlags.IgnoreCase));
            AutomationElement EditButton = twButton.GetFirstChild(vClassCard);
            if (EditButton is null) { return; }

            await Task.Delay(500);
            if (EditButton.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern btn)
            {
                btn.Invoke();
            }

            ClearButton = twClear.GetFirstChild(vClassCard);
            if (ClearButton is not null)
            {
                if (ClearButton.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern clearBtn)
                {
                    clearBtn.Invoke();
                }
            }
        }
    }
}

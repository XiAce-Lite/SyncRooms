using System.Windows;
using System.Windows.Controls;

namespace SyncRooms.Controls
{
    /// <summary>
    /// RoomCardDetail.xaml の相互作用ロジック
    /// </summary>
    public partial class RoomCardDetail : UserControl
    {
        public RoomCardDetail()
        {
            InitializeComponent();
        }

        private void RoomIdCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(RoomId.Text);
        }

        private void EnterRoom_Click(object sender, RoutedEventArgs e)
        {
            Tools.EnterRoom(RoomId.Text);
        }
    }
}

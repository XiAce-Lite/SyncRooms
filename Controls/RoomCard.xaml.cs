using System.Windows;
using System.Windows.Controls;

namespace SyncRooms.Controls
{
    /// <summary>
    /// RoomCard.xaml の相互作用ロジック
    /// </summary>
    public partial class RoomCard : UserControl
    {
        public RoomCard()
        {
            InitializeComponent();
        }

        private void RoomIdCopy_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(RoomId.Text))
            {
                Clipboard.SetDataObject(RoomId.Text);
            }
        }

        private void EnterRoom_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(RoomId.Text))
            {
                Tools.EnterRoom(RoomId.Text);
            }
        }
    }
}

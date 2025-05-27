using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            if (!string.IsNullOrEmpty(RoomId.Text))
            {
                Clipboard.SetDataObject(RoomId.Text);
            }
        }

        private void EnterRoom_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(RoomId.Text) && !string.IsNullOrEmpty(RoomTitle.Text) && NeedPassword.IsChecked != null)
            {
                Tools.EnterRoom(RoomTitle.Text,RoomId.Text, (bool)NeedPassword.IsChecked);
            }
        }

        private void RoomDescription_MouseEnter(object sender, MouseEventArgs e)
        {
            RoomDescriptionPopup.IsOpen = true;
        }

        private void RoomDescription_MouseLeave(object sender, MouseEventArgs e)
        {
            // マウスがPopup上にある場合は閉じない
            if (!RoomDescriptionPopup.IsMouseOver)
                RoomDescriptionPopup.IsOpen = false;
        }

        private void RoomDescriptionPopup_MouseEnter(object sender, MouseEventArgs e)
        {
            RoomDescriptionPopup.IsOpen = true;
        }

        private void RoomDescriptionPopup_MouseLeave(object sender, MouseEventArgs e)
        {
            // マウスがTextBlock上にある場合は閉じない
            if (!RoomDescription.IsMouseOver)
                RoomDescriptionPopup.IsOpen = false;
        }
    }
}

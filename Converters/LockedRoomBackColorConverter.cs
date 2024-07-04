using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SyncRooms.Converters
{
    class LockedRoomBackColorConverter : IValueConverter
    {
        //#d9e2fe デフォルト色

        /// <summary>
        /// Bool値で、NeedPasswd = True の場合は暗く。そうでない場合はデフォルト色を返却。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                throw new ArgumentException("value is null.", nameof(value));
            }

            if (value is not bool) { throw new ArgumentException("value is not bool.", nameof(value)); }

            string color = string.Empty;
            bool b = (bool)value;
            if (b)
            {
                color = "#ffa9a9a9";
            }
            else
            {
                color = "#d9e2fe";
            }
            object obj = ColorConverter.ConvertFromString(color);
            SolidColorBrush ret = new((Color)obj);
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

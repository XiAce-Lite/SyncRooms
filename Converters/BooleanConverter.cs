using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SyncRooms.Converters
{
    // 1. IValueConverterを継承する
    public class InverseBoolConverter : IValueConverter
    {
        // 2.Convertメソッドを実装
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 流れてきた値がboolじゃない時は不正値として変換
            // お好みで例外を投げても良い
            if (value is not bool b) { return DependencyProperty.UnsetValue; }

            // 流れてきたbool値を変換してreturnする
            return !b;
        }

        // 3.ConvertBackメソッドを実装
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ただの反転なのでBinding元に書き戻すときも全く同様の処理で良い
            if (value is not bool b) { return DependencyProperty.UnsetValue; }
            return !b;
        }
    }
}

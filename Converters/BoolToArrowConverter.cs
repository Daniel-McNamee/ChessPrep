using System;
using System.Globalization;
using System.Windows.Data;

namespace ChessProject.Converters
{
    public class BoolToArrowConverter : IValueConverter
    {
        // true  -> ▲
        // false -> ▼
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAscending)
                return isAscending ? "▲" : "▼";

            return "";
        }

        // Not needed for one-way bindings
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

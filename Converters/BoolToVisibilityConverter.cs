using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChessProject.Converters
{
    /// Converts a boolean ViewModel property into Visibility for the UI.
    /// 
    /// Used to show/hide UI elements such as:
    /// - last move square highlight
    /// - active sort column indicators
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool isVisible))
                return Visibility.Collapsed;

            // Reverses the visibility logic if the converter parameter is "Invert"
            if (parameter?.ToString() == "Invert")
                isVisible = !isVisible;

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        // Not needed for one-way bindings
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

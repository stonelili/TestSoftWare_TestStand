using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TestStandClone.UI.Converters
{
    /// <summary>
    /// Converts null/non-null values to Visibility.
    /// Returns Visible when value is not null, Collapsed when null.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverted = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) ?? false;
            bool hasValue = value != null;

            if (isInverted)
            {
                return hasValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

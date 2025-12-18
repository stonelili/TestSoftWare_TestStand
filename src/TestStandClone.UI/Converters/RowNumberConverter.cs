using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace TestStandClone.UI.Converters
{
    /// <summary>
    /// Converter to display the row number in the DataGrid.
    /// </summary>
    public class RowNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataGridRow row)
            {
                return (row.GetIndex() + 1).ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

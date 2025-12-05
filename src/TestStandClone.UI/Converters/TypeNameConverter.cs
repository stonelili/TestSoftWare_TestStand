using System.Globalization;
using System.Windows.Data;

namespace TestStandClone.UI.Converters
{
    /// <summary>
    /// Converter to display a friendly type name.
    /// </summary>
    public class TypeNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var typeName = value.GetType().Name;
                // Remove "Step" suffix for cleaner display
                if (typeName.EndsWith("Step"))
                {
                    typeName = typeName.Substring(0, typeName.Length - 4);
                }
                return typeName;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

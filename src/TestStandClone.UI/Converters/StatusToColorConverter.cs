using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TestStandClone.Core;

namespace TestStandClone.UI.Converters
{
    /// <summary>
    /// Converts StepStatus to a brush color for visual feedback.
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a StepStatus value to a SolidColorBrush.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StepStatus status)
            {
                return status switch
                {
                    StepStatus.Idle => new SolidColorBrush(Colors.Gray),
                    StepStatus.Running => new SolidColorBrush(Colors.DodgerBlue),
                    StepStatus.Passed => new SolidColorBrush(Colors.Green),
                    StepStatus.Failed => new SolidColorBrush(Colors.Red),
                    StepStatus.Error => new SolidColorBrush(Colors.DarkRed),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        /// <summary>
        /// Not implemented - one-way binding only.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

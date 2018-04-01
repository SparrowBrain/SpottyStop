using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SpottyStop.Infrastructure.Converters
{
    public class BoolToCollapsedVisibilityConverter : BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible;
            if (!bool.TryParse(value?.ToString(), out visible))
            {
                return Visibility.Collapsed;
            }

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
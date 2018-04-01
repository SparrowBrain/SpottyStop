using System;
using System.Globalization;
using System.Windows.Data;

namespace SpottyStop.Infrastructure.Converters
{
    public class AfterCurrentToIconStringConverter : BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AfterCurrent afterCurrent;
            if (!Enum.TryParse(value?.ToString(), out afterCurrent))
            {
                return "/Icons/7108.ico";
            }

            string icon;
            switch (afterCurrent)
            {
                case AfterCurrent.NotConnected:
                    icon = "/Icons/7108_notconnected.ico";
                    break;

                case AfterCurrent.Stop:
                    icon = "/Icons/7108_stop.ico";
                    break;

                case AfterCurrent.ShutDown:
                    icon = "/Icons/7108_shutdown.ico";
                    break;

                default:
                    icon = "/Icons/7108.ico";
                    break;
            }

            return icon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
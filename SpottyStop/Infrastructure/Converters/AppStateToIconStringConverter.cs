using System;
using System.Globalization;
using System.Windows.Data;

namespace SpottyStop.Infrastructure.Converters
{
    public class AppStateToIconStringConverter : BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AppState appState;
            if (!Enum.TryParse(value?.ToString(), out appState))
            {
                return "/Icons/7108.ico";
            }

            string icon;
            switch (appState)
            {
                case AppState.NotConnected:
                    icon = "/Icons/7108_notconnected.ico";
                    break;

                case AppState.StopAfterCurrent:
                    icon = "/Icons/7108_stop.ico";
                    break;

                case AppState.ShutDownAfterCurrent:
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
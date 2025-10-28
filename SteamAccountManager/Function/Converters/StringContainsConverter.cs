// Converters/StringContainsConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace SteamAccountManager.Converters
{
    public class StringContainsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value as string;
            string searchTerm = parameter as string;

            if (stringValue != null && searchTerm != null)
            {
                return stringValue.Contains(searchTerm);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
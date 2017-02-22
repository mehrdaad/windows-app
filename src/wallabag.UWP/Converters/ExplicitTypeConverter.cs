using System;
using Windows.UI.Xaml.Data;

namespace wallabag.Converters
{
    public class ExplicitTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => System.Convert.ChangeType(value, targetType);

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => System.Convert.ChangeType(value, targetType);
    }
}

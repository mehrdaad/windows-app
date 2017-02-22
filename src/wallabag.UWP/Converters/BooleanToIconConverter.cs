using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace wallabag.Converters
{
    public class BooleanToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool val = (bool)value;

            if (parameter.ToString() == "read")
            {
                if (val)
                    return CreateFontIcon("\uE18B");
                else
                    return CreateFontIcon("\uE001");
            }
            else
            {
                if (val)
                    return CreateFontIcon("\uE007");
                else
                    return CreateFontIcon("\uE006");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private FontIcon CreateFontIcon(string glyph) => new FontIcon() { Glyph = glyph, FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets") };
    }
}

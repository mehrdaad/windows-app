using Windows.UI;
using Windows.UI.Xaml.Media;

namespace wallabag.Data.Common.Helpers
{
    public static class ColorHelper
    {
        public static SolidColorBrush ToSolidColorBrush(this Color color) => new SolidColorBrush(color);
    }
}

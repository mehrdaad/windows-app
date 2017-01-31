using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace wallabag.Common.Helpers
{
    public static class WebViewHelper
    {
        // Using a DependencyProperty as the backing store for Html.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HtmlProperty =
            DependencyProperty.RegisterAttached("Html", typeof(string), typeof(WebViewHelper), new PropertyMetadata(string.Empty, new PropertyChangedCallback(OnHtmlChanged)));

        public static string GetHtml(DependencyObject obj) => (string)obj.GetValue(HtmlProperty);
        public static void SetHtml(DependencyObject obj, string value) => obj.SetValue(HtmlProperty, value);

        private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wv = d as WebView;
            if (e.NewValue != null)
                wv?.NavigateToString((string)e.NewValue);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace wallabag.Common
{
    public static class IListExtensions
    {
        public static void AddSorted<T>(this IList<T> list, T item, Comparer<T> comparer = null, bool sortAscending = false)
        {
            if (comparer == null)
                comparer = Comparer<T>.Default;

            int i = 0;

            if (sortAscending)
                while (i < list.Count && comparer.Compare(list[i], item) < 0)
                    i++;
            else
                while (i < list.Count && comparer.Compare(list[i], item) > 0)
                    i++;

            list.Insert(i, item);
        }
        public static void Replace<T>(this IList<T> oldList, IList<T> newList)
        {
            oldList.Clear();
            foreach (var item in newList)
                oldList.Add(item);
        }
        public static IEnumerable<T2> Convert<T1, T2>(this IEnumerable<T1> sourceList)
        {
            var newList = new List<T2>();

            foreach (var item in sourceList)
                newList.Add((T2)(dynamic)item);

            return newList;
        }
    }

    public static class StringExtensions
    {
        public static string FormatWith(this string format, object source)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            Regex r = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            string rewrittenFormat = r.Replace(format, delegate (Match m)
            {
                Group startGroup = m.Groups["start"];
                Group propertyGroup = m.Groups["property"];
                Group formatGroup = m.Groups["format"];
                Group endGroup = m.Groups["end"];

                var value = (propertyGroup.Value == null)
                           ? source
                           : source.GetType().GetRuntimeProperty(propertyGroup.Value).GetValue(source);

                return value.ToString();
            });
            return rewrittenFormat;
        }
    }

    public static class WebViewExtensions
    {
        // Using a DependencyProperty as the backing store for Html.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HtmlProperty =
            DependencyProperty.RegisterAttached("Html", typeof(string), typeof(WebViewExtensions), new PropertyMetadata(string.Empty, new PropertyChangedCallback(OnHtmlChanged)));

        public static string GetHtml(DependencyObject obj) => (string)obj.GetValue(HtmlProperty);
        public static void SetHtml(DependencyObject obj, string value) => obj.SetValue(HtmlProperty, value);

        private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WebView wv = d as WebView;
            if (e.NewValue != null)
                wv?.NavigateToString((string)e.NewValue);
        }
    }

    public static class TitleBarExtensions
    {
        public static readonly DependencyProperty ForegroundColorProperty =
          DependencyProperty.RegisterAttached("ForegroundColor", typeof(Color),
          typeof(TitleBarExtensions),
          new PropertyMetadata(null, OnForegroundColorPropertyChanged));

        public static Color GetForegroundColor(DependencyObject d)
        {
            return (Color)d.GetValue(ForegroundColorProperty);
        }

        public static void SetForegroundColor(DependencyObject d, Color value)
        {
            d.SetValue(ForegroundColorProperty, value);
        }

        private static void OnForegroundColorPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var color = (Color)e.NewValue;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ForegroundColor = color;
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.RegisterAttached("BackgroundColor", typeof(Color),
            typeof(TitleBarExtensions),
            new PropertyMetadata(null, OnBackgroundColorPropertyChanged));

        public static Color GetBackgroundColor(DependencyObject d)
        {
            return (Color)d.GetValue(BackgroundColorProperty);
        }

        public static void SetBackgroundColor(DependencyObject d, Color value)
        {
            d.SetValue(BackgroundColorProperty, value);
        }

        private static void OnBackgroundColorPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var color = (Color)e.NewValue;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = color;
        }

        public static readonly DependencyProperty ButtonForegroundColorProperty =
            DependencyProperty.RegisterAttached("ButtonForegroundColor", typeof(Color),
            typeof(TitleBarExtensions),
            new PropertyMetadata(null, OnButtonForegroundColorPropertyChanged));

        public static Color GetButtonForegroundColor(DependencyObject d)
        {
            return (Color)d.GetValue(ButtonForegroundColorProperty);
        }

        public static void SetButtonForegroundColor(DependencyObject d, Color value)
        {
            d.SetValue(ButtonForegroundColorProperty, value);
        }

        private static void OnButtonForegroundColorPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var color = (Color)e.NewValue;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonForegroundColor = color;
        }

        public static readonly DependencyProperty ButtonBackgroundColorProperty =
            DependencyProperty.RegisterAttached("ButtonBackgroundColor", typeof(Color),
            typeof(TitleBarExtensions),
            new PropertyMetadata(null, OnButtonBackgroundColorPropertyChanged));

        public static Color GetButtonBackgroundColor(DependencyObject d)
        {
            return (Color)d.GetValue(ButtonBackgroundColorProperty);
        }

        public static void SetButtonBackgroundColor(DependencyObject d, Color value)
        {
            d.SetValue(ButtonBackgroundColorProperty, value);
        }

        private static void OnButtonBackgroundColorPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var color = (Color)e.NewValue;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = color;
        }

        public static readonly DependencyProperty IsVisibleProperty =
         DependencyProperty.RegisterAttached("IsVisible", typeof(bool),
         typeof(TitleBarExtensions),
         new PropertyMetadata(true, OnIsVisiblePropertyChanged));

        public static bool GetIsVisible(DependencyObject d)
        {
            return (bool)d.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(DependencyObject d, bool value)
        {
            d.SetValue(IsVisibleProperty, value);
        }

        private static void OnIsVisiblePropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var isExtended = !(bool)e.NewValue;

            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = isExtended;
        }
    }

}

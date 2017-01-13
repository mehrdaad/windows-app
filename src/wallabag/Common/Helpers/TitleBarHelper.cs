using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace wallabag.Common.Helpers
{
    public static class TitleBarHelper
    {
        public static readonly DependencyProperty ForegroundColorProperty =
          DependencyProperty.RegisterAttached("ForegroundColor", typeof(Color),
          typeof(TitleBarHelper),
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

            if (GeneralHelper.IsPhone)
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = color;
            }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.RegisterAttached("BackgroundColor", typeof(Color),
            typeof(TitleBarHelper),
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

            if (GeneralHelper.IsPhone)
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.BackgroundColor = color;
                statusBar.BackgroundOpacity = 0.8;
            }
        }

        public static readonly DependencyProperty ButtonForegroundColorProperty =
            DependencyProperty.RegisterAttached("ButtonForegroundColor", typeof(Color),
            typeof(TitleBarHelper),
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
            typeof(TitleBarHelper),
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
         typeof(TitleBarHelper),
         new PropertyMetadata(true, OnIsVisiblePropertyChangedAsync));

        public static bool GetIsVisible(DependencyObject d)
        {
            return (bool)d.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(DependencyObject d, bool value)
        {
            d.SetValue(IsVisibleProperty, value);
        }

        private static async void OnIsVisiblePropertyChangedAsync(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            bool isExtended = !(bool)e.NewValue;

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = isExtended;

            if (GeneralHelper.IsPhone)
            {
                var statusBar = StatusBar.GetForCurrentView();
                if (isExtended)
                    await statusBar.HideAsync();
                else
                    await statusBar.ShowAsync();
            }
        }

        public static Task ResetAsync()
        {
            return CoreWindow.GetForCurrentThread().Dispatcher.RunTaskAsync(async () =>
            {
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;

                coreTitleBar.ExtendViewIntoTitleBar = false;
                titleBar.BackgroundColor = null;
                titleBar.ButtonBackgroundColor = null;
                titleBar.ForegroundColor = null;
                titleBar.ButtonForegroundColor = null;

                if (GeneralHelper.IsPhone)
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    await statusBar.ShowAsync();
                }
            });
        }
    }
}

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace wallabag.Common.Helpers
{
    public static class TitleBarHelper
    {
        public static void SetForegroundColor(Color color)
        {
            ApplicationView.GetForCurrentView().TitleBar.ForegroundColor = color;

            if (GeneralHelper.IsPhone)
                StatusBar.GetForCurrentView().ForegroundColor = color;
        }
        public static void SetBackgroundColor(Color color)
        {
            ApplicationView.GetForCurrentView().TitleBar.BackgroundColor = color;

            if (GeneralHelper.IsPhone)
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.BackgroundColor = color;
                statusBar.BackgroundOpacity = 0.8;
            }
        }

        public static void SetButtonForegroundColor(Color color)
            => ApplicationView.GetForCurrentView().TitleBar.ButtonForegroundColor = color;
        public static void SetButtonBackgroundColor(Color color)
            => ApplicationView.GetForCurrentView().TitleBar.ButtonBackgroundColor = color;

        public static async Task SetVisibilityAsync(Visibility visibility)
        {
            bool isExtended = visibility == Visibility.Collapsed;

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

        public static async Task ResetToDefaultsAsync()
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
        }
    }
}

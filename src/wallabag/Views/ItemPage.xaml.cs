using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using wallabag.Common.Helpers;
using wallabag.Services;
using wallabag.ViewModels;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using WindowsStateTriggers;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class ItemPage : Page
    {
        private const string _scriptName = "changeHtmlAttributes";
        private bool _isCommandBarVisible = false;
        private bool _isCommandBarCompact = false;

        public ItemPageViewModel ViewModel { get { return DataContext as ItemPageViewModel; } }

        public ItemPage()
        {
            InitializeComponent();

            HtmlViewer.ScriptNotify += HtmlViewer_ScriptNotifyAsync;
            HtmlViewer.NavigationStarting += HtmlViewer_NavigationStartingAsync;

            ShowMinimalCommandBarStoryboard.Completed += (s, e) =>
            {
                _isCommandBarVisible = true;
                _isCommandBarCompact = true;
            };
            ShowFullCommandBarStoryboard.Completed += (s, e) =>
            {
                _isCommandBarVisible = true;
                _isCommandBarCompact = false;
            };
            HideCommandBarStoryboard.Completed += (s, e) =>
            {
                _isCommandBarVisible = false;
                _isCommandBarCompact = false;
            };

            (ViewModel as INotifyPropertyChanged).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.FailureHasHappened))
                    VisualStateManager.GoToState(this, nameof(ErrorState), false);
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var titleBarBackgroundRectangle = FindName(nameof(TitleBarBackgroundRectangle)) as Rectangle;
            System.Diagnostics.Debug.WriteLine(GeneralHelper.DeviceFamilyOfCurrentDevice);
            if (SettingsService.Instance.WhiteOverlayForTitleBar &&
                GeneralHelper.DeviceFamilyOfCurrentDevice == DeviceFamily.Desktop)
            {
                titleBarBackgroundRectangle.Visibility = Visibility.Visible;
            }
            else
                titleBarBackgroundRectangle.Visibility = Visibility.Collapsed;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e) => HtmlViewer.NavigateToString("<p></p>");

        private MenuFlyout _rightClickMenuFlyout;
        private Grid _rightClickMenuGrid;
        private async void HtmlViewer_ScriptNotifyAsync(object sender, NotifyEventArgs e)
        {
            if (e.Value == "finishedReading")
            {
                if (!_isCommandBarVisible)
                    ShowMinimalCommandBarStoryboard.Begin();

            }
            else
            {
                var notify = e.Value.Split("|"[0]);

                switch (notify[0])
                {
                    case "S":
                        ViewModel.Item.Model.ReadingProgress = double.Parse(notify[1], NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo);
                        if (_isCommandBarCompact && ViewModel.Item.Model.ReadingProgress < 99)
                        {
                            _isCommandBarCompact = false;
                            HideCommandBarStoryboard.Begin();
                        }
                        break;
                    case "RC":
                    case "LC":
                        int x = int.Parse(notify[2]);
                        int y = int.Parse(notify[3]);
                        try
                        {
                            ViewModel.RightClickUri = new Uri(notify[1]);
                            ShowRightClickContextMenu(x, y);
                        }
                        catch { }
                        break;
                    case "video-app":
                    case "video-browser":
                        string provider = notify[1];
                        string videoId = notify[2];

                        var launcherOptions = new LauncherOptions();

                        if (notify[0] == "video-app")
                            launcherOptions = new LauncherOptions() { FallbackUri = GetVideoUri(provider, videoId, true) };

                        await Launcher.LaunchUriAsync(GetVideoUri(provider, videoId), launcherOptions);
                        break;
                    default:
                        break;
                }
            }
        }

        private Uri GetVideoUri(string provider, string videoId, bool returnFallbackUri = false)
        {
            string uriString = string.Empty;
            var openMode = SettingsService.Instance.VideoOpenMode;

            if (provider == "youtube")
                if (openMode == SettingsService.WallabagVideoOpenMode.App && returnFallbackUri == false)
                    uriString = $"vnd.youtube:{videoId}";
                else
                    uriString = $"https://youtu.be/{videoId}";
            else if (provider == "vimeo")
                if (openMode == SettingsService.WallabagVideoOpenMode.App && returnFallbackUri == false)
                    uriString = $"vimeo://v/{videoId}";
                else
                    uriString = $"https://vimeo.com/{videoId}";
            else
                uriString = videoId;

            return new Uri(uriString);
        }

        private void ShowRightClickContextMenu(int x, int y)
        {
            if (_rightClickMenuFlyout == null)
                _rightClickMenuFlyout = Resources["RightClickMenuFlyout"] as MenuFlyout;

            _rightClickMenuFlyout.ShowAt(HtmlViewer, new Point(x, y));
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) => (_rightClickMenuGrid.Resources["SaveRightClickLinkStoryboard"] as Storyboard).Begin();
        private void RightClickGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _rightClickMenuGrid = sender as Grid;
            (_rightClickMenuGrid.Resources["SaveRightClickLinkStoryboard"] as Storyboard).Completed += (s, args) => _rightClickMenuFlyout.Hide();
        }

        private async void HtmlViewer_NavigationStartingAsync(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null)
            {
                args.Cancel = true;
                await Launcher.LaunchUriAsync(args.Uri);
            }
        }

        private async void IncreaseFontSizeAsync(object sender, RoutedEventArgs e)
        {
            ViewModel.FontSize += 1;
            await InvokeChangeHtmlAttributesAsync();
        }
        private async void DecreaseFontSizeAsync(object sender, RoutedEventArgs e)
        {
            ViewModel.FontSize -= 1;
            await InvokeChangeHtmlAttributesAsync();
        }
        private async void ChangeColorSchemeAsync(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button == lightThemeButton)
                ViewModel.ColorScheme = "light";
            else if (button == sepiaThemeButton)
                ViewModel.ColorScheme = "sepia";
            else if (button == darkThemeButton)
                ViewModel.ColorScheme = "dark";
            else if (button == blackThemeButton)
                ViewModel.ColorScheme = "black";

            await InvokeChangeHtmlAttributesAsync();
        }
        private async void ChangeFontFamilyAsync(object sender, RoutedEventArgs e)
        {
            if (ViewModel.FontFamily.Equals("serif"))
                ViewModel.FontFamily = "sans";
            else
                ViewModel.FontFamily = "serif";
            await InvokeChangeHtmlAttributesAsync();
        }
        private async void ChangeTextAlignmentAsync(object sender, RoutedEventArgs e)
        {
            var senderButton = sender as Button;

            if (ViewModel.TextAlignment.Equals("left"))
            {
                ViewModel.TextAlignment = "justify";
                senderButton.Content = "\uE1A1";
            }
            else
            {
                ViewModel.TextAlignment = "left";
                senderButton.Content = "\uE1A2";
            }

            await InvokeChangeHtmlAttributesAsync();
        }

        private IAsyncOperation<string> InvokeChangeHtmlAttributesAsync()
        {
            var arguments = new List<string>
            {
                ViewModel.ColorScheme,
                ViewModel.FontFamily,
                ViewModel.FontSize.ToString(),
                ViewModel.TextAlignment
            };
            ViewModel.UpdateBrushes();
            return HtmlViewer.InvokeScriptAsync(_scriptName, arguments);
        }

        private void OpenCommandsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCommandBarVisible || _isCommandBarCompact)
                ShowFullCommandBarStoryboard.Begin();
            else
                HideCommandBarStoryboard.Begin();

            if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent(nameof(AppBarButton), nameof(AppBarButton.LabelPosition)))
            {
                lightThemeButton.LabelPosition = CommandBarLabelPosition.Default;
                sepiaThemeButton.LabelPosition = CommandBarLabelPosition.Default;
                darkThemeButton.LabelPosition = CommandBarLabelPosition.Default;
                blackThemeButton.LabelPosition = CommandBarLabelPosition.Default;
            }
        }

        private void HtmlViewer_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
                ShowHtmlViewerStoryboard.Begin();
        }
    }
}

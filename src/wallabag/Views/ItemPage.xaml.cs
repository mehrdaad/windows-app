using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using wallabag.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

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
            this.InitializeComponent();

            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.IsVisibleChanged += (s, e) => backButton.Visibility = s.IsVisible ? Visibility.Visible : Visibility.Collapsed;

            HtmlViewer.ScriptNotify += HtmlViewer_ScriptNotify;
            HtmlViewer.NavigationStarting += HtmlViewer_NavigationStarting;

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

        private MenuFlyout _rightClickMenuFlyout;
        private Grid _rightClickMenuGrid;
        private void HtmlViewer_ScriptNotify(object sender, NotifyEventArgs e)
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
                        var x = int.Parse(notify[2]);
                        var y = int.Parse(notify[3]);
                        ViewModel.RightClickUri = new Uri(notify[1]);
                        ShowRightClickContextMenu(x, y);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ShowRightClickContextMenu(int x, int y)
        {
            if (_rightClickMenuFlyout == null)
            {
                _rightClickMenuFlyout = Resources["RightClickMenuFlyout"] as MenuFlyout;
                _rightClickMenuFlyout.Closed += (s, e) =>
                {
                    (_rightClickMenuGrid.Resources["ResetRightClickLinkStoryboard"] as Storyboard).Begin();
                };
            }

            _rightClickMenuFlyout.ShowAt(HtmlViewer, new Point(x, y));
        }

        private void saveButton_Click(object sender, RoutedEventArgs e) => (_rightClickMenuGrid.Resources["SaveRightClickLinkStoryboard"] as Storyboard).Begin();
        private void rightClickGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _rightClickMenuGrid = sender as Grid;
            (_rightClickMenuGrid.Resources["SaveRightClickLinkStoryboard"] as Storyboard).Completed += (s, args) => _rightClickMenuFlyout.Hide();
        }

        private async void HtmlViewer_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null)
            {
                args.Cancel = true;
                await Launcher.LaunchUriAsync(args.Uri);
            }
        }

        private async void IncreaseFontSize(object sender, RoutedEventArgs e)
        {
            ViewModel.FontSize += 1;
            await InvokeChangeHtmlAttributesAsync();
        }
        private async void DecreaseFontSize(object sender, RoutedEventArgs e)
        {
            ViewModel.FontSize -= 1;
            await InvokeChangeHtmlAttributesAsync();
        }
        private async void ChangeColorSchemeToLight(object sender, RoutedEventArgs e)
        {
            ViewModel.ColorScheme = "light";
            await InvokeChangeHtmlAttributesAsync();
        }
        private async void ChangeColorSchemeToDark(object sender, RoutedEventArgs e)
        {
            ViewModel.ColorScheme = "dark";
            await InvokeChangeHtmlAttributesAsync();
        }
        private async void ChangeFontFamily(object sender, RoutedEventArgs e)
        {
            if (ViewModel.FontFamily.Equals("serif"))
                ViewModel.FontFamily = "sans";
            else
                ViewModel.FontFamily = "serif";
            await InvokeChangeHtmlAttributesAsync();
        }
        private async void ChangeTextAlignment(object sender, RoutedEventArgs e)
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
            var arguments = new List<string>();
            arguments.Add(ViewModel.ColorScheme);
            arguments.Add(ViewModel.FontFamily);
            arguments.Add(ViewModel.FontSize.ToString());
            arguments.Add(ViewModel.TextAlignment);

            ViewModel.UpdateBrushes();
            return HtmlViewer.InvokeScriptAsync(_scriptName, arguments);
        }

        private void openCommandsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCommandBarVisible || _isCommandBarCompact)
                ShowFullCommandBarStoryboard.Begin();
            else
                HideCommandBarStoryboard.Begin();
        }
    }
}

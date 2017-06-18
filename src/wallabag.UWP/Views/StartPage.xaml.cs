using wallabag.Common.Helpers;
using wallabag.ViewModels;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace wallabag.Views
{
    public sealed partial class StartPage : Page
    {
        public StartPageViewModel ViewModel => (DataContext as StartPageViewModel);
        public StartPage() => InitializeComponent();

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await TitleBarHelper.SetVisibilityAsync(Windows.UI.Xaml.Visibility.Collapsed);
            TitleBarHelper.SetButtonBackgroundColor(Colors.Transparent);
            TitleBarHelper.SetButtonForegroundColor(Colors.White);
        }
    }
}

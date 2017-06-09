using wallabag.ViewModels;
using Windows.UI.Xaml.Controls;

namespace wallabag.Views
{
    public sealed partial class StartPage : Page
    {
        public StartPageViewModel ViewModel => (DataContext as StartPageViewModel);
        public StartPage() => InitializeComponent();
    }
}

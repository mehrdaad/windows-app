using wallabag.ViewModels;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class ShareTargetPage : Page
    {
        public AddItemViewModel ViewModel { get { return DataContext as AddItemViewModel; } }
        public ShareTargetPage()
        {
            InitializeComponent();
            ViewModel.AddingStarted += (s, e) => AddStoryboard.Begin();
        }
    }
}

using System;
using wallabag.Data.ViewModels;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class ShareTargetPage : Page
    {
        private ShareOperation _shareOperation;

        public AddItemViewModel ViewModel => DataContext as AddItemViewModel;
        public ShareTargetPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _shareOperation = e.Parameter as ShareOperation;
            var link = await _shareOperation.Data.GetWebLinkAsync();

            ViewModel.UriString = link.ToString();
        }
    }
}

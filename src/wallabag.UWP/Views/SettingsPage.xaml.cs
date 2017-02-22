using wallabag.Common;
using wallabag.Data.ViewModels;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPageViewModel ViewModel => DataContext as SettingsPageViewModel;

        public bool WhiteOverlayForTitleBar
        {
            get { return Settings.CustomSettings.WhiteOverlayForTitleBar; }
            set { Settings.CustomSettings.WhiteOverlayForTitleBar = value; }
        }

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void VideoOpenModeRadioButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (sender == BrowserVideoOpenModeRadioButton)
                ViewModel.SetVideoOpenMode(Data.Common.Settings.Reading.WallabagVideoOpenMode.Browser);
            else if (sender == AppVideoOpenModeRadioButton)
                ViewModel.SetVideoOpenMode(Data.Common.Settings.Reading.WallabagVideoOpenMode.App);
            else if (sender == InlineVideoOpenModeRadioButton)
                ViewModel.SetVideoOpenMode(Data.Common.Settings.Reading.WallabagVideoOpenMode.Inline);
        }
    }
}

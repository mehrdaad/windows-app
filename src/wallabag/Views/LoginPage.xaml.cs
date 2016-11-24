using GalaSoft.MvvmLight.Messaging;
using wallabag.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        public LoginPageViewModel ViewModel { get { return this.DataContext as LoginPageViewModel; } }

        public LoginPage()
        {
            this.InitializeComponent();

            ShowAlertGridStoryboard.Completed += (s, e) => HideAlertGridStoryboard.Begin();
        }

        private void IgnorePointerWheel(object sender, PointerRoutedEventArgs e) => e.Handled = true;
        private void IgnoreTouchManipulation(object sender, ManipulationStartingRoutedEventArgs e) => e.Handled = true;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Messenger.Default.Register<NotificationMessage>(this, message =>
            {
                ShowAlertGridStoryboard.Begin();
                AlertDescriptionTextBlock.Text = message.Notification;
            });
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e) => Messenger.Default.Unregister(this);
    }
}

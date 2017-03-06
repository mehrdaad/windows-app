using GalaSoft.MvvmLight.Messaging;
using wallabag.Data.ViewModels;
using Windows.UI.Core;
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
        public LoginPageViewModel ViewModel => DataContext as LoginPageViewModel;

        public LoginPage()
        {
            InitializeComponent();

            ShowAlertGridStoryboard.Completed += (s, e) => HideAlertGridStoryboard.Begin();
        }

        private void LoginPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;

            if (ViewModel.PreviousCommand.CanExecute(null))
                ViewModel.CurrentStep--;
        }

        private void IgnorePointerWheel(object sender, PointerRoutedEventArgs e) => e.Handled = true;
        private void IgnoreTouchManipulation(object sender, ManipulationStartingRoutedEventArgs e) => e.Handled = true;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var snm = SystemNavigationManager.GetForCurrentView();
            snm.BackRequested -= App.GlobalBackRequested;
            snm.BackRequested += LoginPage_BackRequested;

            Messenger.Default.Register<NotificationMessage>(this, message =>
            {
                ShowAlertGridStoryboard.Begin();
                AlertDescriptionTextBlock.Text = message.Notification;
            });

            ViewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName.Equals(nameof(ViewModel.CurrentStep)))
                {
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                        ViewModel.PreviousCommand.CanExecute(null) ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
                }
            };
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Messenger.Default.Unregister(this);
            SystemNavigationManager.GetForCurrentView().BackRequested -= LoginPage_BackRequested;
            SystemNavigationManager.GetForCurrentView().BackRequested += App.GlobalBackRequested;
        }


    }
}

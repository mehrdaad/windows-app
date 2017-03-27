using GalaSoft.MvvmLight.Ioc;
using wallabag.Api;
using wallabag.Data.Common;
using wallabag.Data.Services;
using wallabag.Data.ViewModels;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Dialogs
{
    public sealed partial class LoginDialog : ContentDialog
    {
        public LoginPageViewModel ViewModel => DataContext as LoginPageViewModel;

        public LoginDialog() => InitializeComponent();

        private async void ContentDialog_PrimaryButtonClickAsync(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var logging = SimpleIoc.Default.GetInstance<ILoggingService>();
            logging.WriteLine("Trying a re-login with given parameters.");

            ViewModel.ClientId = Settings.Authentication.ClientId;
            ViewModel.ClientSecret = Settings.Authentication.ClientSecret;
            ViewModel.Url = Settings.Authentication.WallabagUri.ToString();

            bool result = await ViewModel.TestConfigurationAsync();
            if (result)
            {
                logging.WriteLine("Re-login successful. Updating settings.");
                var client = SimpleIoc.Default.GetInstance<IWallabagClient>();

                Settings.Authentication.AccessToken = client.AccessToken;
                Settings.Authentication.RefreshToken = client.RefreshToken;
                Settings.Authentication.LastTokenRefreshDateTime = client.LastTokenRefreshDateTime;
            }
            else
                logging.WriteLine("Re-login failed.");
        }
    }
}

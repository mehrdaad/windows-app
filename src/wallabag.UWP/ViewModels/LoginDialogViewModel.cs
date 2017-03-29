using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Data.Common;
using wallabag.Data.Services;
using wallabag.Data.ViewModels;

namespace wallabag.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class LoginDialogViewModel : ViewModelBase
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;

        public RelayCommand ReLoginCommand { get; private set; }

        public event EventHandler ReloginCompleted;
        public event EventHandler ReloginFailed;

        public LoginDialogViewModel() => ReLoginCommand = new RelayCommand(async () => await ReloginAsync());

        private async Task ReloginAsync()
        {
            IsActive = true;

            var logging = SimpleIoc.Default.GetInstance<ILoggingService>();
            logging.WriteLine("Trying a re-login with given parameters.");

            var loginViewModel = SimpleIoc.Default.GetInstance<LoginPageViewModel>();
            loginViewModel.ClientId = Settings.Authentication.ClientId;
            loginViewModel.ClientSecret = Settings.Authentication.ClientSecret;
            loginViewModel.Url = Settings.Authentication.WallabagUri.ToString();
            loginViewModel.UseCustomSettings = true;
            loginViewModel.Username = Username;
            loginViewModel.Password = Password;

            bool result = await loginViewModel.TestConfigurationAsync();

            if (result)
            {
                logging.WriteLine("Re-login successful. Updating settings.");
                var client = SimpleIoc.Default.GetInstance<IWallabagClient>();

                Settings.Authentication.AccessToken = client.AccessToken;
                Settings.Authentication.RefreshToken = client.RefreshToken;
                Settings.Authentication.LastTokenRefreshDateTime = client.LastTokenRefreshDateTime;

                ReloginCompleted?.Invoke(this, null);
            }
            else
            {
                logging.WriteLine("Re-login failed.");
                ReloginFailed?.Invoke(this, null);
            }

            IsActive = false;
        }
    }
}

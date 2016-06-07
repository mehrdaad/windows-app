using PropertyChanged;
using System;
using System.Threading.Tasks;
using Template10.Mvvm;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class LoginPageViewModel : ViewModelBase
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        public bool IsTestRunning { get; set; }
        private bool _TestIsSuccessful = false;

        public DelegateCommand TestConfigurationCommand { get; private set; }
        public DelegateCommand ContinueCommand { get; private set; }

        public LoginPageViewModel()
        {
            TestConfigurationCommand = new DelegateCommand(async () =>
            {
                _TestIsSuccessful = await TestConfigurationAsync();
                ContinueCommand.RaiseCanExecuteChanged();
            }, () => TestConfigurationCanExecute());
            ContinueCommand = new DelegateCommand(async () => await ContinueAsync(), () => _TestIsSuccessful);

            PropertyChanged += (s, e) => TestConfigurationCommand.RaiseCanExecuteChanged();
        }

        private bool TestConfigurationCanExecute()
        {
            if (!string.IsNullOrWhiteSpace(Username) &&
                !string.IsNullOrWhiteSpace(Password) &&
                !string.IsNullOrWhiteSpace(Url) &&
                !string.IsNullOrWhiteSpace(ClientId) &&
                !string.IsNullOrWhiteSpace(ClientSecret) &&
                ClientId.Length == 52 &&
                ClientSecret.Length == 50)
                return true;
            else
                return false;
        }
        private Task<bool> TestConfigurationAsync()
        {
            IsTestRunning = true;
            App.Client.ClientId = ClientId;
            App.Client.ClientSecret = ClientSecret;
            App.Client.InstanceUri = new Uri(Url);

            return (App.Client.RequestTokenAsync(Username, Password).ContinueWith(x => { IsTestRunning = false; return x.Result; }));
        }
        private async Task ContinueAsync()
        {

        }
    }
}

using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Api.Models;
using wallabag.Models;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Navigation;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class LoginPageViewModel : ViewModelBase
    {
        public bool CredentialsAreExisting { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        public bool IsTestRunning { get; set; }
        private bool _TestIsSuccessful = false;

        public event EventHandler ConfigurationTestFailed;
        public event EventHandler ConfigurationTestSucceeded;
        public event EventHandler ContinueStarted;
        public event EventHandler ContinueCompleted;

        public DelegateCommand TestConfigurationCommand { get; private set; }
        public DelegateCommand ContinueCommand { get; private set; }

        public LoginPageViewModel()
        {
            TestConfigurationCommand = new DelegateCommand(async () =>
            {
                _TestIsSuccessful = await TestConfigurationAsync();

                if (_TestIsSuccessful)
                    ConfigurationTestSucceeded?.Invoke(this, new EventArgs());
                else
                    ConfigurationTestFailed?.Invoke(this, new EventArgs());

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
        private async Task ContinueAsync(bool credentialsExist = false)
        {
            ContinueStarted?.Invoke(this, new EventArgs());

            if (!credentialsExist)
            {
                Services.SettingsService.Instance.ClientId = ClientId;
                Services.SettingsService.Instance.ClientSecret = ClientSecret;
                Services.SettingsService.Instance.WallabagUrl = new Uri(Url);
                Services.SettingsService.Instance.AccessToken = App.Client.AccessToken;
                Services.SettingsService.Instance.RefreshToken = App.Client.RefreshToken;
                Services.SettingsService.Instance.LastTokenRefreshDateTime = App.Client.LastTokenRefreshDateTime;
            }

            var itemResponse = await App.Client.GetItemsWithEnhancedMetadataAsync(ItemsPerPage: 1000);
            var items = itemResponse.Items as List<WallabagItem>;

            // For users with a lot of items
            if (itemResponse.Pages > 1)
                for (int i = 1; i < itemResponse.Pages; i++)
                    items.AddRange(await App.Client.GetItemsAsync(ItemsPerPage: 1000));

            var tags = await App.Client.GetTagsAsync();

            await Task.Factory.StartNew(() =>
            {
                foreach (var item in items)
                    App.Database.Insert((Item)item);

                foreach (var tag in tags)
                    App.Database.Insert((Tag)tag);

            }, TaskCreationOptions.LongRunning);

            ContinueCompleted?.Invoke(this, new EventArgs());

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = null;
            titleBar.ButtonForegroundColor = null;

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = false;

            NavigationService.Navigate(typeof(Views.MainPage));
            NavigationService.ClearHistory();
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if (parameter is bool)
            {
                CredentialsAreExisting = (bool)parameter;
                await ContinueAsync(true);
            }
        }
    }
}

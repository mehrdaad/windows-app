using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Api.Models;
using wallabag.Data.Common;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;

namespace wallabag.Data.ViewModels
{
    public class LoginPageViewModel : ViewModelBase
    {
        private readonly ILoggingService _loggingService;
        private readonly INavigationService _navigationService;
        private readonly IPlatformSpecific _device;
        private readonly IWallabagClient _client;
        private readonly IApiClientCreationService _apiClientService;
        private readonly SQLiteConnection _database;

        private bool _restoredFromPageState = false;

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Url { get; set; } = "https://";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public bool? UseCustomSettings { get; set; } = false;
        public bool? AllowCollectionOfTelemetryData { get; set; } = true;

        public int CurrentStep { get; set; } = 0;
        public string ProgressDescription { get; set; } = string.Empty;

        [DependsOn(nameof(SelectedProvider))]
        public bool UrlFieldIsVisible => SelectedProvider == WallabagProvider.GetOther(_device);

        public List<WallabagProvider> Providers { get; set; }
        public WallabagProvider SelectedProvider { get; set; }

        public bool CameraIsSupported { get; set; } = false;

        public RelayCommand PreviousCommand { get; private set; }
        public RelayCommand NextCommand { get; private set; }
        public RelayCommand RegisterCommand { get; private set; }
        public RelayCommand WhatIsWallabagCommand { get; private set; }
        public RelayCommand ScanQRCodeCommand { get; private set; }

        public LoginPageViewModel(
            ILoggingService logging,
            INavigationService navigation,
            IPlatformSpecific device,
            IWallabagClient client,
            IApiClientCreationService apiService,
            SQLiteConnection database)
        {
            _loggingService = logging;
            _navigationService = navigation;
            _device = device;
            _client = client;
            _apiClientService = apiService;
            _database = database;

            _loggingService.WriteLine("Creating new instance of LoginPageViewModel.");

            Providers = new List<WallabagProvider>()
            {
                //new WallabagProvider(new Uri("https://framabag.org"), "framabag", Device.GetLocalizedResource("FramabagProviderDescription")),
                new WallabagProvider(new Uri("https://app.wallabag.it"), "wallabag.it", _device.GetLocalizedResource("WallabagItProviderDescription")),
                new WallabagProvider(new Uri("http://v2.wallabag.org"), "v2.wallabag.org", _device.GetLocalizedResource("V2WallabagOrgProviderDescription")),
                WallabagProvider.GetOther(_device)
            };

            PreviousCommand = new RelayCommand(() => Previous(), () => PreviousCanBeExecuted());
            NextCommand = new RelayCommand(async () => await NextAsync(), () => NextCanBeExecuted());
            RegisterCommand = new RelayCommand(() => _device.LaunchUri((SelectedProvider as WallabagProvider).Url.Append("/register")), () => RegistrationCanBeExecuted());
            WhatIsWallabagCommand = new RelayCommand(() => _device.LaunchUri(new Uri("vimeo://v/167435064"), new Uri("https://vimeo.com/167435064")));
            ScanQRCodeCommand = new RelayCommand(() => _navigationService.Navigate(Navigation.Pages.QRScanPage));

            this.PropertyChanged += This_PropertyChanged;
        }

        private void This_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PreviousCommand.RaiseCanExecuteChanged();
            NextCommand.RaiseCanExecuteChanged();
            RegisterCommand.RaiseCanExecuteChanged();
        }

        private bool PreviousCanBeExecuted() => CurrentStep > 0 && CurrentStep != 2;
        private bool NextCanBeExecuted()
        {
            return (SelectedProvider != null || _restoredFromPageState) &&
                    CurrentStep <= 3 &&
                    CurrentStep != 2;
        }
        private bool RegistrationCanBeExecuted()
        {
            var selectedProvider = SelectedProvider as WallabagProvider;

            return selectedProvider != null && selectedProvider.Url != null;
        }

        private void Previous()
        {
            _loggingService.WriteLine($"Navigating one step back. New step: {CurrentStep}");

            PreviousCommand.RaiseCanExecuteChanged();
            NextCommand.RaiseCanExecuteChanged();

            if (CurrentStep != 3)
                CurrentStep -= 1;
            else if (CurrentStep == 3)
                CurrentStep = 1;
        }
        private async Task NextAsync()
        {
            var selectedProvider = SelectedProvider as WallabagProvider;

            _loggingService.WriteLine($"Navigating one step forward. New step: {CurrentStep}");
            _loggingService.WriteLine($"Selected provider URL: {selectedProvider?.Url}");

            if (CurrentStep == 0)
            {
                if (Url.IsValidUri() == false)
                    Url = selectedProvider?.Url == null ? "https://" : selectedProvider.Url.ToString();

                CurrentStep += 1;
                return;
            }

            if (CurrentStep == 1)
            {
                if (!Url.IsValidUri())
                {
                    Messenger.Default.Send(new NotificationMessage(_device.GetLocalizedResource("UrlFormatWrongMessage")));
                    _loggingService.WriteLine($"URL was in a wrong format. Input: {Url}", LoggingCategory.Warning);
                    return;
                }

                CurrentStep += 1;
            }

            if (CurrentStep == 2)
            {
                if (await TestConfigurationAsync())
                    await DownloadAndSaveItemsAndTagsAsync();
                else
                {
                    CurrentStep = 1;
                    Messenger.Default.Send(new NotificationMessage(_device.GetLocalizedResource("CredentialsWrongMessage")));
                    _loggingService.WriteLine("The entered credentials are wrong.");
                    return;
                }

                CurrentStep += 1;
                return;
            }

            if (CurrentStep == 3)
            {
                _loggingService.WriteLine("Save fetched values to settings.");
                Settings.Authentication.AccessToken = _client.AccessToken;
                Settings.Authentication.RefreshToken = _client.RefreshToken;
                Settings.Authentication.WallabagUri = _client.InstanceUri;
                Settings.Authentication.LastTokenRefreshDateTime = _client.LastTokenRefreshDateTime;
                Settings.Authentication.ClientId = _client.ClientId;
                Settings.Authentication.ClientSecret = _client.ClientSecret;
                Settings.General.AllowCollectionOfTelemetryData = (bool)AllowCollectionOfTelemetryData;

                _navigationService.Navigate(Navigation.Pages.MainPage);
                _navigationService.ClearHistory();
            }
        }

        public async Task<bool> TestConfigurationAsync()
        {
            string urlToTest = Url;

            _loggingService.WriteLine("Testing configuration.");
            _loggingService.WriteLine($"URL: {urlToTest}");
            ProgressDescription = _device.GetLocalizedResource("TestingConfigurationMessage");

            if (!urlToTest.StartsWith("https://") && !urlToTest.StartsWith("http://"))
            {
                _loggingService.WriteLine("URL doesn't start with protocol, using HTTPS.");
                urlToTest = "https://" + urlToTest;
            }

            if (!urlToTest.EndsWith("/"))
            {
                urlToTest += "/";
                _loggingService.WriteLine("URL doesn't end with a slash. Added it.");
            }

            Url = urlToTest;
            _loggingService.WriteLine($"URL to test: {urlToTest}");

            try { await new HttpClient().GetAsync(new Uri(urlToTest)); }
            catch
            {
                _loggingService.WriteLine("Server was not reachable.", LoggingCategory.Info);
                return false;
            }
            
            _loggingService.WriteLineIf(UseCustomSettings == true, "User wants to use custom settings.");
            if (UseCustomSettings == false || (
                    UseCustomSettings == true &&
                    string.IsNullOrWhiteSpace(ClientSecret) &&
                    string.IsNullOrWhiteSpace(ClientId)
                ))
            {
                bool clientCreationIsSuccessful = await TestClientCreationWithUrlAsync(new Uri(urlToTest));
                if (!clientCreationIsSuccessful && urlToTest.StartsWith("https://"))
                {
                    _loggingService.WriteLine("Client creation was not successful. Trying again with HTTP.");
                    urlToTest = urlToTest.Replace("https://", "http://");

                    clientCreationIsSuccessful = await TestClientCreationWithUrlAsync(new Uri(urlToTest));
                }

                if (!clientCreationIsSuccessful)
                {
                    _loggingService.WriteLine("Client creation failed.");
                    return false;
                }
            }

            _client.ClientId = ClientId;
            _client.ClientSecret = ClientSecret;
            _client.InstanceUri = new Uri(urlToTest);

            _loggingService.WriteLine("Request authentication tokens.");
            bool result = await _client.RequestTokenAsync(Username, Password).ContinueWith(x =>
            {
                if (x.Exception == null)
                    return x.Result;
                else
                    return false;
            });
            _loggingService.WriteLineIf(result, "Authentication tokens successful requested.");

            if (result == false && urlToTest.StartsWith("https://"))
            {
                _loggingService.WriteLine("Authentication tokens couldn't be requested. Trying again with HTTP.");

                urlToTest = urlToTest.Replace("https://", "http://");
                _client.InstanceUri = new Uri(urlToTest);

                result = await _client.RequestTokenAsync(Username, Password).ContinueWith(x =>
                {
                    if (x.Exception == null)
                        return x.Result;
                    else
                        return false;
                });
                _loggingService.WriteLineIf(result == false, "Authentication tokens couldn't be requested.");
            }

            if (result)
                Url = urlToTest;

            return result;
        }

        private async Task<bool> TestClientCreationWithUrlAsync(Uri urlToTest)
        {
            _client.InstanceUri = urlToTest;
            _loggingService.WriteLine($"Instance URI of the client: {_client.InstanceUri}");

            var clientData = await _apiClientService.CreateClientAsync(urlToTest.ToString(), Username, Password);
            if (clientData.Success)
            {
                _loggingService.WriteLine("Client creation was successful.");
                ClientId = clientData.Id;
                ClientSecret = clientData.Secret;
            }

            return clientData.Success;
        }

        public async Task DownloadAndSaveItemsAndTagsAsync()
        {
            _loggingService.WriteLine("Downloading items and tags...");
            ProgressDescription = _device.GetLocalizedResource("DownloadingItemsTextBlock.Text");
            int itemsPerPage = 100;

            var itemResponse = await _client.GetItemsWithEnhancedMetadataAsync(itemsPerPage: itemsPerPage);

            _loggingService.WriteLine($"Total number of items: {itemResponse.TotalNumberOfItems}");
            _loggingService.WriteLine($"Pages: {itemResponse.Pages}");

            var items = itemResponse?.Items as List<WallabagItem>;

            // For users with a lot of items
            if (itemResponse.Pages > 1)
            {
                for (int i = 2; i <= itemResponse.Pages; i++)
                {
                    _loggingService.WriteLine($"Downloading items for page {i}...");
                    ProgressDescription = string.Format(_device.GetLocalizedResource("DownloadingItemsWithProgress"), items.Count, itemResponse.TotalNumberOfItems);
                    items.AddRange(await _client.GetItemsAsync(itemsPerPage: itemsPerPage, pageNumber: i));
                }
            }

            _loggingService.WriteLine("Downloading tags.");
            var tags = await _client.GetTagsAsync();

            ProgressDescription = _device.GetLocalizedResource("SavingItemsInDatabaseMessage");

            _loggingService.WriteLine("Saving results in database.");
            await Task.Run(() =>
            {
                _database.RunInTransaction(() =>
                {
                    _loggingService.WriteLine("Saving items in database.");
                    foreach (var item in items)
                        _database.InsertOrReplace((Item)item);

                    _loggingService.WriteLine("Saving tags in database.");
                    foreach (var tag in tags)
                        _database.InsertOrReplace((Tag)tag);
                });
            });
        }

        public override async Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state)
        {
            CameraIsSupported = await _device.GetHasCameraAsync();
            _loggingService.WriteLine($"Camera is supported: {CameraIsSupported}");

            if (state.Count > 0)
            {
                _loggingService.WriteLine("PageState.Count was greater than zero. Restoring.");
                _loggingService.WriteObject(state);

                Username = state[nameof(Username)] as string;
                Password = state[nameof(Password)] as string;
                Url = state[nameof(Url)] as string;
                ClientId = state[nameof(ClientId)] as string;
                ClientSecret = state[nameof(ClientSecret)] as string;

                CurrentStep = (int)state[nameof(CurrentStep)];
                UseCustomSettings = (bool?)state[nameof(UseCustomSettings)];

                _restoredFromPageState = true;
            }

            _loggingService.WriteLine("Checking for navigation parameter.");
            ProtocolSetupNavigationParameter protocolSetupParameter = null;

            if (parameter is ProtocolSetupNavigationParameter)
            {
                protocolSetupParameter = parameter as ProtocolSetupNavigationParameter;
                _loggingService.WriteLine($"Parameter existing. Setting it as {nameof(protocolSetupParameter)}.");
            }

            // TODO: Handle the activation via the QR code
            //if (SessionState.ContainsKey(QRScanPageViewModel.m_QRRESULTKEY))
            //    protocolSetupParameter = SessionState[QRScanPageViewModel.m_QRRESULTKEY] as ProtocolSetupNavigationParameter;

            if (protocolSetupParameter != null)
            {
                _loggingService.WriteLine($"Making use of the {nameof(ProtocolSetupNavigationParameter)}.");

                Username = protocolSetupParameter.Username;
                Url = protocolSetupParameter.Server;

                SelectedProvider = WallabagProvider.GetOther(_device);

                CurrentStep = 1;
            }
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState)
        {
            _loggingService.WriteLine("Saving page state.");

            pageState[nameof(Username)] = Username;
            pageState[nameof(Password)] = Password;
            pageState[nameof(Url)] = Url;
            pageState[nameof(ClientId)] = ClientId;
            pageState[nameof(ClientSecret)] = ClientSecret;

            pageState[nameof(CurrentStep)] = CurrentStep;
            pageState[nameof(UseCustomSettings)] = UseCustomSettings;

            return Task.FromResult(true);
        }
    }
}

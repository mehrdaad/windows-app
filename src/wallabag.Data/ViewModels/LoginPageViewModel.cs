using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using wallabag.Api.Models;
using wallabag.Data.Common;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Models;
using wallabag.Data.Services;
using Windows.Devices.Enumeration;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace wallabag.Data.ViewModels
{
    public class LoginPageViewModel : ViewModelBase
    {
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
        public Visibility UrlFieldVisibility => ((SelectedProvider as WallabagProvider)?.Url == null) ? Visibility.Visible : Visibility.Collapsed;

        public List<WallabagProvider> Providers { get; set; }
        public object SelectedProvider { get; set; }

        public RelayCommand PreviousCommand { get; private set; }
        public RelayCommand NextCommand { get; private set; }
        public RelayCommand RegisterCommand { get; private set; }
        public ICommand WhatIsWallabagCommand { get; private set; }
        public ICommand ScanQRCodeCommand { get; private set; }

        public bool CameraIsSupported { get; set; } = false;

        public LoginPageViewModel()
        {
            _loggingService.WriteLine("Creating new instance of LoginPageViewModel.");

            Providers = new List<WallabagProvider>()
            {
                //new WallabagProvider(new Uri("https://framabag.org"), "framabag", GeneralHelper.LocalizedResource("FramabagProviderDescription")),
                new WallabagProvider(new Uri("https://app.wallabag.it"), "wallabag.it", GeneralHelper.LocalizedResource("WallabagItProviderDescription")),
                new WallabagProvider(new Uri("http://v2.wallabag.org"), "v2.wallabag.org", GeneralHelper.LocalizedResource("V2WallabagOrgProviderDescription")),
                WallabagProvider.Other
            };

            PreviousCommand = new RelayCommand(() => Previous(), () => PreviousCanBeExecuted());
            NextCommand = new RelayCommand(async () => await NextAsync(), () => NextCanBeExecuted());
            RegisterCommand = new RelayCommand(async () => await Launcher.LaunchUriAsync((SelectedProvider as WallabagProvider).Url.Append("/register")),
                () => RegistrationCanBeExecuted());
            WhatIsWallabagCommand = new RelayCommand(async () => await Launcher.LaunchUriAsync(new Uri("vimeo://v/167435064"), new LauncherOptions() { FallbackUri = new Uri("https://vimeo.com/167435064") }));
            ScanQRCodeCommand = new RelayCommand(() => _navigationService.Navigate(Navigation.Pages.QRScanPage));

            this.PropertyChanged += This_PropertyChanged;
        }

        private void This_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PreviousCommand.RaiseCanExecuteChanged();
            NextCommand.RaiseCanExecuteChanged();
            RegisterCommand.RaiseCanExecuteChanged();

            if (e.PropertyName == nameof(CurrentStep))
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = PreviousCanBeExecuted() ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
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
                try { var x = new Uri(Url); }
                catch (UriFormatException)
                {
                    Messenger.Default.Send(new NotificationMessage(GeneralHelper.LocalizedResource("UrlFormatWrongMessage")));
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
                    Messenger.Default.Send(new NotificationMessage(GeneralHelper.LocalizedResource("CredentialsWrongMessage")));
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

        private async Task<bool> TestConfigurationAsync()
        {
            _loggingService.WriteLine("Testing configuration.");
            _loggingService.WriteLine($"URL: {Url}");
            ProgressDescription = GeneralHelper.LocalizedResource("TestingConfigurationMessage");

            if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
            {
                _loggingService.WriteLine("URL doesn't start with protocol, using HTTPS.");
                Url = "https://" + Url;
            }

            if (!Url.EndsWith("/"))
            {
                Url += "/";
                _loggingService.WriteLine("URL doesn't end with a slash. Added it.");
            }

            _loggingService.WriteLine($"URL to test: {Url}");

            try { await new HttpClient().GetAsync(new Uri(Url)); }
            catch
            {
                _loggingService.WriteLine("Server was not reachable.", LoggingCategory.Info);
                return false;
            }

            _loggingService.WriteLineIf(UseCustomSettings == true, "User wants to use custom settings.");
            if (UseCustomSettings == false)
            {
                _client.InstanceUri = new Uri(Url);
                _loggingService.WriteLine($"Instance URI of the client: {_client.InstanceUri}");

                bool clientCreationIsSuccessful = await CreateApiClientAsync();
                _loggingService.WriteLineIf(clientCreationIsSuccessful, "Client creation is successful.");

                if (clientCreationIsSuccessful == false &&
                    Url.StartsWith("https://"))
                {
                    _loggingService.WriteLine("Client creation was not successful. Trying again with HTTP.");
                    Url = Url.Replace("https://", "http://");
                    _client.InstanceUri = new Uri(Url);

                    if (await CreateApiClientAsync() == false)
                    {
                        _loggingService.WriteLine("Client creation failed.");
                        return false;
                    }
                }
            }

            _client.ClientId = ClientId;
            _client.ClientSecret = ClientSecret;
            _client.InstanceUri = new Uri(Url);

            _loggingService.WriteLine("Request authentication tokens.");
            bool result = await _client.RequestTokenAsync(Username, Password).ContinueWith(x =>
            {
                if (x.Exception == null)
                    return x.Result;
                else
                    return false;
            });
            _loggingService.WriteLineIf(result, "Authentication tokens successful requested.");

            if (result == false && Url.StartsWith("https://"))
            {
                _loggingService.WriteLine("Authentication tokens couldn't be requested. Trying again with HTTP.");

                Url = Url.Replace("https://", "http://");
                _client.InstanceUri = new Uri(Url);

                result = await _client.RequestTokenAsync(Username, Password).ContinueWith(x =>
                {
                    if (x.Exception == null)
                        return x.Result;
                    else
                        return false;
                });
                _loggingService.WriteLineIf(result == false, "Authentication tokens couldn't be requested.");
            }

            return result;
        }
        private async Task DownloadAndSaveItemsAndTagsAsync()
        {
            _loggingService.WriteLine("Downloading items and tags...");
            ProgressDescription = GeneralHelper.LocalizedResource("DownloadingItemsTextBlock.Text");
            int itemsPerPage = 100;

            var itemResponse = await _client.GetItemsWithEnhancedMetadataAsync(itemsPerPage: itemsPerPage);

            _loggingService.WriteLine($"Total number of items: {itemResponse.TotalNumberOfItems}");
            _loggingService.WriteLine($"Pages: {itemResponse.Pages}");

            var items = itemResponse.Items as List<WallabagItem>;

            // For users with a lot of items
            if (itemResponse.Pages > 1)
            {
                for (int i = 2; i <= itemResponse.Pages; i++)
                {
                    _loggingService.WriteLine($"Downloading items for page {i}...");
                    ProgressDescription = string.Format(GeneralHelper.LocalizedResource("DownloadingItemsWithProgress"), items.Count, itemResponse.TotalNumberOfItems);
                    items.AddRange(await _client.GetItemsAsync(itemsPerPage: itemsPerPage, pageNumber: i));
                }
            }

            _loggingService.WriteLine("Downloading tags.");
            var tags = await _client.GetTagsAsync();

            ProgressDescription = GeneralHelper.LocalizedResource("SavingItemsInDatabaseMessage");

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

        private void BackRequested(object sender, BackRequestedEventArgs e)
        {
            _loggingService.WriteLine("User navigated back using the back button.");

            e.Handled = true;
            if (PreviousCanBeExecuted())
                Previous();
        }

        public override async Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += BackRequested;

            CameraIsSupported = (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).Count > 0;
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

            if (SessionState.ContainsKey(QRScanPageViewModel.m_QRRESULTKEY))
                protocolSetupParameter = SessionState[QRScanPageViewModel.m_QRRESULTKEY] as ProtocolSetupNavigationParameter;

            if (protocolSetupParameter != null)
            {
                _loggingService.WriteLine($"Making use of the {nameof(ProtocolSetupNavigationParameter)}.");

                Username = protocolSetupParameter.Username;
                Url = protocolSetupParameter.Server;

                SelectedProvider = WallabagProvider.Other;

                CurrentStep = 1;
                return;
            }
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= BackRequested;
            _loggingService.WriteLine("Saving page state.");

            pageState[nameof(Username)] = Username;
            pageState[nameof(Password)] = Password;
            pageState[nameof(Url)] = Url;
            pageState[nameof(ClientId)] = ClientId;
            pageState[nameof(ClientSecret)] = ClientSecret;

            pageState[nameof(CurrentStep)] = CurrentStep;
            pageState[nameof(UseCustomSettings)] = UseCustomSettings;

            return Task.CompletedTask;
        }

        #region Client creation

        HttpClient _http;

        private const string m_LOGINSTARTSTRING = "<input type=\"hidden\" name=\"_csrf_token\" value=\"";
        private const string m_TOKENSTARTSTRING = "<input type=\"hidden\" id=\"client__token\" name=\"client[_token]\" value=\"";
        private const string m_FINALTOKENSTARTSTRING = "<strong><pre>";
        private const string m_FINALTOKENENDSTRING = "</pre></strong>";
        private const string m_HTMLINPUTENDSTRING = "\" />";

        public async Task<bool> CreateApiClientAsync()
        {
            _loggingService.WriteLine("Creating a new client...");
            ProgressDescription = GeneralHelper.LocalizedResource("CreatingClientMessage");

            string token = string.Empty;
            bool useNewApi = false;
            int step = 1;
            HttpResponseMessage message = null;

            try
            {
                _http = new HttpClient();
                var instanceUri = new Uri(Url);

                _loggingService.WriteLine("Logging in to get a cookie... (mmh, cookies...)");
                _loggingService.WriteLine($"URI: {instanceUri.Append("/login_check")}");

                // Step 1: Login to get a cookie.
                var loginContent = new HttpStringContent($"_username={System.Net.WebUtility.UrlEncode(Username)}&_password={System.Net.WebUtility.UrlEncode(Password)}&_csrf_token={await GetCsrfTokenAsync()}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                var loginResponse = await _http.PostAsync(instanceUri.Append("/login_check"), loginContent);

                if (!loginResponse.IsSuccessStatusCode)
                {
                    _loggingService.WriteLine($"Failed. Resulted content: {await loginResponse.Content.ReadAsStringAsync()}", LoggingCategory.Warning);
                    return false;
                }

                // Step 2: Get the client token
                _loggingService.WriteLine("Get the client token...");
                step++;
                var clientCreateUri = instanceUri.Append("/developer/client/create");
                token = await GetStringFromHtmlSequenceAsync(clientCreateUri, m_TOKENSTARTSTRING, m_HTMLINPUTENDSTRING);

                _loggingService.WriteLine($"URI: {clientCreateUri}");
                _loggingService.WriteLine($"Token: {token}");

                // Step 3: Create the new client
                _loggingService.WriteLine("Creating the new client...");
                step++;
                string stringContent = string.Empty;
                useNewApi = (await _client.GetVersionAsync()).Minor > 0;

                _loggingService.WriteLine($"Use new API: {useNewApi}");

                stringContent = $"client[redirect_uris]={GetRedirectUri(useNewApi)}&client[save]=&client[_token]={token}";

                if (useNewApi)
                    stringContent = $"client[name]={new EasClientDeviceInformation().FriendlyName}&" + stringContent;

                _loggingService.WriteLine($"Content: {stringContent}");

                var addContent = new HttpStringContent(stringContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                var addResponse = _http.PostAsync(clientCreateUri, addContent);

                message = await addResponse;

                if (!message.IsSuccessStatusCode)
                {
                    _loggingService.WriteLine($"Failed. Resulted content: {await message.Content.ReadAsStringAsync()}", LoggingCategory.Warning);
                    return false;
                }

                string content = await message.Content.ReadAsStringAsync();
                _loggingService.WriteLine($"Parsing the resulted string: {content}");

                var result = ParseResult(content, useNewApi);
                if (result != null)
                {
                    _loggingService.WriteLine("Success!");
                    ClientId = result.Id;
                    ClientSecret = result.Secret;

                    _http.Dispose();
                }
                else
                    return false;

                return true;
            }
            catch (Exception e)
            {
                _loggingService.TrackException(e);
                return false;
            }
        }

        private object GetRedirectUri(bool useNewApi) => useNewApi ? default(Uri) : new Uri(Url).Append(new EasClientDeviceInformation().FriendlyName);
        private Task<string> GetCsrfTokenAsync() => GetStringFromHtmlSequenceAsync(new Uri(Url).Append("/login"), m_LOGINSTARTSTRING, m_HTMLINPUTENDSTRING);

        private async Task<string> GetStringFromHtmlSequenceAsync(Uri uri, string startString, string endString)
        {
            _loggingService.WriteLine("Trying to get a string from HTML code.");
            _loggingService.WriteLine($"URI: {uri}");
            _loggingService.WriteLine($"Start string: {startString}");
            _loggingService.WriteLine($"End string: {endString}");

            string html = await (await _http.GetAsync(uri)).Content.ReadAsStringAsync();
            _loggingService.WriteLine($"HTML to parse: {html}");

            int startIndex = html.IndexOf(startString) + startString.Length;
            int endIndex = html.IndexOf(endString, startIndex);

            _loggingService.WriteLine($"Start index: {startIndex}");
            _loggingService.WriteLine($"End index: {endIndex}");

            string result = html.Substring(startIndex, endIndex - startIndex);
            _loggingService.WriteLine($"Result: {result}");

            return result;
        }

        private ClientResultData ParseResult(string html, bool useNewApi = false)
        {
            _loggingService.WriteLine("Trying to parse the resulted client credentials...");
            _loggingService.WriteLine($"Use new API: {useNewApi}");
            _loggingService.WriteLine($"HTML to parse: {html}");

            try
            {
                var results = new List<string>();
                int resultCount = useNewApi ? 2 : 1;

                int lastIndex = 0;

                do
                {
                    int start = html.IndexOf(m_FINALTOKENSTARTSTRING, lastIndex) + m_FINALTOKENSTARTSTRING.Length;
                    lastIndex = html.IndexOf(m_FINALTOKENENDSTRING, start);

                    _loggingService.WriteLine($"Start index: {start}");
                    _loggingService.WriteLine($"Last index: {lastIndex}");

                    string result = html.Substring(start, lastIndex - start);

                    _loggingService.WriteLine($"Result: {result}");

                    results.Add(result);

                    _loggingService.WriteLine($"Number of results: {results.Count}");

                } while (results.Count <= resultCount);

                var finalResult = default(ClientResultData);
                if (useNewApi)
                    finalResult = new ClientResultData()
                    {
                        Name = results[0],
                        Id = results[1],
                        Secret = results[2]
                    };
                else
                    finalResult = new ClientResultData()
                    {
                        Id = results[0],
                        Secret = results[1],
                        Name = string.Empty
                    };

                _loggingService.WriteObject(finalResult);
                return finalResult;
            }
            catch (Exception e)
            {
                _loggingService.TrackException(e);
                Messenger.Default.Send(new NotificationMessage(GeneralHelper.LocalizedResource("SomethingWentWrongMessage")));
                return null;
            }
        }

        #endregion
    }

    public class ClientResultData
    {
        public string Id { get; set; }
        public string Secret { get; set; }
        public string Name { get; set; }
    }
}

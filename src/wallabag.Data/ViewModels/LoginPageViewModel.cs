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
            LoggingService.WriteLine("Creating new instance of LoginPageViewModel.");

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
            ScanQRCodeCommand = new RelayCommand(() => Navigation.NavigateTo(Pages.QRScanPage));

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
            LoggingService.WriteLine($"Navigating one step back. New step: {CurrentStep}");

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

            LoggingService.WriteLine($"Navigating one step forward. New step: {CurrentStep}");
            LoggingService.WriteLine($"Selected provider URL: {selectedProvider?.Url}");

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
                    LoggingService.WriteLine($"URL was in a wrong format. Input: {Url}", LoggingCategory.Warning);
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
                    LoggingService.WriteLine("The entered credentials are wrong.");
                    return;
                }

                CurrentStep += 1;
                return;
            }

            if (CurrentStep == 3)
            {
                LoggingService.WriteLine("Save fetched values to settings.");
                Settings.Authentication.AccessToken = Client.AccessToken;
                Settings.Authentication.RefreshToken = Client.RefreshToken;
                Settings.Authentication.WallabagUri = Client.InstanceUri;
                Settings.Authentication.LastTokenRefreshDateTime = Client.LastTokenRefreshDateTime;
                Settings.Authentication.ClientId = Client.ClientId;
                Settings.Authentication.ClientSecret = Client.ClientSecret;
                Settings.General.AllowCollectionOfTelemetryData = (bool)AllowCollectionOfTelemetryData;

                Navigation.NavigateTo(Pages.MainPage);
                Navigation.ClearHistory();
            }
        }

        private async Task<bool> TestConfigurationAsync()
        {
            LoggingService.WriteLine("Testing configuration.");
            LoggingService.WriteLine($"URL: {Url}");
            ProgressDescription = GeneralHelper.LocalizedResource("TestingConfigurationMessage");

            if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
            {
                LoggingService.WriteLine("URL doesn't start with protocol, using HTTPS.");
                Url = "https://" + Url;
            }

            if (!Url.EndsWith("/"))
            {
                Url += "/";
                LoggingService.WriteLine("URL doesn't end with a slash. Added it.");
            }

            LoggingService.WriteLine($"URL to test: {Url}");

            try { await new HttpClient().GetAsync(new Uri(Url)); }
            catch
            {
                LoggingService.WriteLine("Server was not reachable.", LoggingCategory.Info);
                return false;
            }

            LoggingService.WriteLineIf(UseCustomSettings == true, "User wants to use custom settings.");
            if (UseCustomSettings == false)
            {
                Client.InstanceUri = new Uri(Url);
                LoggingService.WriteLine($"Instance URI of the client: {Client.InstanceUri}");

                bool clientCreationIsSuccessful = await CreateApiClientAsync();
                LoggingService.WriteLineIf(clientCreationIsSuccessful, "Client creation is successful.");

                if (clientCreationIsSuccessful == false &&
                    Url.StartsWith("https://"))
                {
                    LoggingService.WriteLine("Client creation was not successful. Trying again with HTTP.");
                    Url = Url.Replace("https://", "http://");
                    Client.InstanceUri = new Uri(Url);

                    if (await CreateApiClientAsync() == false)
                    {
                        LoggingService.WriteLine("Client creation failed.");
                        return false;
                    }
                }
            }

            Client.ClientId = ClientId;
            Client.ClientSecret = ClientSecret;
            Client.InstanceUri = new Uri(Url);

            LoggingService.WriteLine("Request authentication tokens.");
            bool result = await Client.RequestTokenAsync(Username, Password).ContinueWith(x =>
            {
                if (x.Exception == null)
                    return x.Result;
                else
                    return false;
            });
            LoggingService.WriteLineIf(result, "Authentication tokens successful requested.");

            if (result == false && Url.StartsWith("https://"))
            {
                LoggingService.WriteLine("Authentication tokens couldn't be requested. Trying again with HTTP.");

                Url = Url.Replace("https://", "http://");
                Client.InstanceUri = new Uri(Url);

                result = await Client.RequestTokenAsync(Username, Password).ContinueWith(x =>
                {
                    if (x.Exception == null)
                        return x.Result;
                    else
                        return false;
                });
                LoggingService.WriteLineIf(result == false, "Authentication tokens couldn't be requested.");
            }

            return result;
        }
        private async Task DownloadAndSaveItemsAndTagsAsync()
        {
            LoggingService.WriteLine("Downloading items and tags...");
            ProgressDescription = GeneralHelper.LocalizedResource("DownloadingItemsTextBlock.Text");
            int itemsPerPage = 100;

            var itemResponse = await Client.GetItemsWithEnhancedMetadataAsync(itemsPerPage: itemsPerPage);

            LoggingService.WriteLine($"Total number of items: {itemResponse.TotalNumberOfItems}");
            LoggingService.WriteLine($"Pages: {itemResponse.Pages}");

            var items = itemResponse.Items as List<WallabagItem>;

            // For users with a lot of items
            if (itemResponse.Pages > 1)
            {
                for (int i = 2; i <= itemResponse.Pages; i++)
                {
                    LoggingService.WriteLine($"Downloading items for page {i}...");
                    ProgressDescription = string.Format(GeneralHelper.LocalizedResource("DownloadingItemsWithProgress"), items.Count, itemResponse.TotalNumberOfItems);
                    items.AddRange(await Client.GetItemsAsync(itemsPerPage: itemsPerPage, pageNumber: i));
                }
            }

            LoggingService.WriteLine("Downloading tags.");
            var tags = await Client.GetTagsAsync();

            ProgressDescription = GeneralHelper.LocalizedResource("SavingItemsInDatabaseMessage");

            LoggingService.WriteLine("Saving results in database.");
            await Task.Run(() =>
            {
                Database.RunInTransaction(() =>
                {
                    LoggingService.WriteLine("Saving items in database.");
                    foreach (var item in items)
                        Database.InsertOrReplace((Item)item);

                    LoggingService.WriteLine("Saving tags in database.");
                    foreach (var tag in tags)
                        Database.InsertOrReplace((Tag)tag);
                });
            });
        }

        private void BackRequested(object sender, BackRequestedEventArgs e)
        {
            LoggingService.WriteLine("User navigated back using the back button.");

            e.Handled = true;
            if (PreviousCanBeExecuted())
                Previous();
        }

        public override async Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += BackRequested;

            CameraIsSupported = (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).Count > 0;
            LoggingService.WriteLine($"Camera is supported: {CameraIsSupported}");

            if (state.Count > 0)
            {
                LoggingService.WriteLine("PageState.Count was greater than zero. Restoring.");
                LoggingService.WriteObject(state);

                Username = state[nameof(Username)] as string;
                Password = state[nameof(Password)] as string;
                Url = state[nameof(Url)] as string;
                ClientId = state[nameof(ClientId)] as string;
                ClientSecret = state[nameof(ClientSecret)] as string;

                CurrentStep = (int)state[nameof(CurrentStep)];
                UseCustomSettings = (bool?)state[nameof(UseCustomSettings)];

                _restoredFromPageState = true;
            }

            LoggingService.WriteLine("Checking for navigation parameter.");
            ProtocolSetupNavigationParameter protocolSetupParameter = null;

            if (parameter is ProtocolSetupNavigationParameter)
            {
                protocolSetupParameter = parameter as ProtocolSetupNavigationParameter;
                LoggingService.WriteLine($"Parameter existing. Setting it as {nameof(protocolSetupParameter)}.");
            }

            // TODO: Implement SessionState
            /* if (SessionState.ContainsKey(QRScanPageViewModel.QRResultKey))
                protocolSetupParameter = SessionState[QRScanPageViewModel.QRResultKey] as ProtocolSetupNavigationParameter; */

            if (protocolSetupParameter != null)
            {
                LoggingService.WriteLine($"Making use of the {nameof(ProtocolSetupNavigationParameter)}.");

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
            LoggingService.WriteLine("Saving page state.");

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

        private const string m_loginStartString = "<input type=\"hidden\" name=\"_csrf_token\" value=\"";
        private const string m_tokenStartString = "<input type=\"hidden\" id=\"client__token\" name=\"client[_token]\" value=\"";
        private const string m_finalTokenStartString = "<strong><pre>";
        private const string m_finalTokenEndString = "</pre></strong>";
        private const string m_htmlInputEndString = "\" />";

        public async Task<bool> CreateApiClientAsync()
        {
            LoggingService.WriteLine("Creating a new client...");
            ProgressDescription = GeneralHelper.LocalizedResource("CreatingClientMessage");

            string token = string.Empty;
            bool useNewApi = false;
            int step = 1;
            HttpResponseMessage message = null;

            try
            {
                _http = new HttpClient();
                var instanceUri = new Uri(Url);

                LoggingService.WriteLine("Logging in to get a cookie... (mmh, cookies...)");
                LoggingService.WriteLine($"URI: {instanceUri.Append("/login_check")}");

                // Step 1: Login to get a cookie.
                var loginContent = new HttpStringContent($"_username={System.Net.WebUtility.UrlEncode(Username)}&_password={System.Net.WebUtility.UrlEncode(Password)}&_csrf_token={await GetCsrfTokenAsync()}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                var loginResponse = await _http.PostAsync(instanceUri.Append("/login_check"), loginContent);

                if (!loginResponse.IsSuccessStatusCode)
                {
                    LoggingService.WriteLine($"Failed. Resulted content: {await loginResponse.Content.ReadAsStringAsync()}", LoggingCategory.Warning);
                    return false;
                }

                // Step 2: Get the client token
                LoggingService.WriteLine("Get the client token...");
                step++;
                var clientCreateUri = instanceUri.Append("/developer/client/create");
                token = await GetStringFromHtmlSequenceAsync(clientCreateUri, m_tokenStartString, m_htmlInputEndString);

                LoggingService.WriteLine($"URI: {clientCreateUri}");
                LoggingService.WriteLine($"Token: {token}");

                // Step 3: Create the new client
                LoggingService.WriteLine("Creating the new client...");
                step++;
                string stringContent = string.Empty;
                useNewApi = (await Client.GetVersionAsync()).Minor > 0;

                LoggingService.WriteLine($"Use new API: {useNewApi}");

                stringContent = $"client[redirect_uris]={GetRedirectUri(useNewApi)}&client[save]=&client[_token]={token}";

                if (useNewApi)
                    stringContent = $"client[name]={new EasClientDeviceInformation().FriendlyName}&" + stringContent;

                LoggingService.WriteLine($"Content: {stringContent}");

                var addContent = new HttpStringContent(stringContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                var addResponse = _http.PostAsync(clientCreateUri, addContent);

                message = await addResponse;

                if (!message.IsSuccessStatusCode)
                {
                    LoggingService.WriteLine($"Failed. Resulted content: {await message.Content.ReadAsStringAsync()}", LoggingCategory.Warning);
                    return false;
                }

                string content = await message.Content.ReadAsStringAsync();
                LoggingService.WriteLine($"Parsing the resulted string: {content}");

                var result = ParseResult(content, useNewApi);
                if (result != null)
                {
                    LoggingService.WriteLine("Success!");
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
                LoggingService.TrackException(e);
                return false;
            }
        }

        private object GetRedirectUri(bool useNewApi) => useNewApi ? default(Uri) : new Uri(Url).Append(new EasClientDeviceInformation().FriendlyName);
        private Task<string> GetCsrfTokenAsync() => GetStringFromHtmlSequenceAsync(new Uri(Url).Append("/login"), m_loginStartString, m_htmlInputEndString);

        private async Task<string> GetStringFromHtmlSequenceAsync(Uri uri, string startString, string endString)
        {
            LoggingService.WriteLine("Trying to get a string from HTML code.");
            LoggingService.WriteLine($"URI: {uri}");
            LoggingService.WriteLine($"Start string: {startString}");
            LoggingService.WriteLine($"End string: {endString}");

            string html = await (await _http.GetAsync(uri)).Content.ReadAsStringAsync();
            LoggingService.WriteLine($"HTML to parse: {html}");

            int startIndex = html.IndexOf(startString) + startString.Length;
            int endIndex = html.IndexOf(endString, startIndex);

            LoggingService.WriteLine($"Start index: {startIndex}");
            LoggingService.WriteLine($"End index: {endIndex}");

            string result = html.Substring(startIndex, endIndex - startIndex);
            LoggingService.WriteLine($"Result: {result}");

            return result;
        }

        private ClientResultData ParseResult(string html, bool useNewApi = false)
        {
            LoggingService.WriteLine("Trying to parse the resulted client credentials...");
            LoggingService.WriteLine($"Use new API: {useNewApi}");
            LoggingService.WriteLine($"HTML to parse: {html}");

            try
            {
                var results = new List<string>();
                int resultCount = useNewApi ? 2 : 1;

                int lastIndex = 0;

                do
                {
                    int start = html.IndexOf(m_finalTokenStartString, lastIndex) + m_finalTokenStartString.Length;
                    lastIndex = html.IndexOf(m_finalTokenEndString, start);

                    LoggingService.WriteLine($"Start index: {start}");
                    LoggingService.WriteLine($"Last index: {lastIndex}");

                    string result = html.Substring(start, lastIndex - start);

                    LoggingService.WriteLine($"Result: {result}");

                    results.Add(result);

                    LoggingService.WriteLine($"Number of results: {results.Count}");

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

                LoggingService.WriteObject(finalResult);
                return finalResult;
            }
            catch (Exception e)
            {
                LoggingService.TrackException(e);
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

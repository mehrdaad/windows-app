using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Template10.Controls;
using Template10.Mvvm;
using wallabag.Api.Models;
using wallabag.Common.Helpers;
using wallabag.Models;
using wallabag.Services;
using Windows.Devices.Enumeration;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class LoginPageViewModel : ViewModelBase
    {
        private bool _restoredFromPageState = false;
        private Frame _oldFrame;

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Url { get; set; } = "https://";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public bool? UseCustomSettings { get; set; } = false;
        public bool? AllowCollectionOfTelemetryData { get; set; } = true;

        public int CurrentStep { get; set; } = 0;
        public string ProgressDescription { get; set; }

        [DependsOn(nameof(SelectedProvider))]
        public Visibility UrlFieldVisibility { get { return ((SelectedProvider as WallabagProvider)?.Url == null) ? Visibility.Visible : Visibility.Collapsed; } }

        public List<WallabagProvider> Providers { get; set; }
        public object SelectedProvider { get; set; }

        public DelegateCommand PreviousCommand { get; private set; }
        public DelegateCommand NextCommand { get; private set; }
        public DelegateCommand RegisterCommand { get; private set; }
        public DelegateCommand WhatIsWallabagCommand { get; private set; }
        public DelegateCommand ScanQRCodeCommand { get; private set; }

        public bool CameraIsSupported { get; set; } = false;

        public LoginPageViewModel()
        {
            Providers = new List<WallabagProvider>()
            {
                //new WallabagProvider(new Uri("https://framabag.org"), "framabag", GeneralHelper.LocalizedResource("FramabagProviderDescription")),
                new WallabagProvider(new Uri("https://app.wallabag.it"), "wallabag.it", GeneralHelper.LocalizedResource("WallabagItProviderDescription")),
                new WallabagProvider(new Uri("http://v2.wallabag.org"), "v2.wallabag.org", GeneralHelper.LocalizedResource("V2WallabagOrgProviderDescription")),
                WallabagProvider.Other
            };

            PreviousCommand = new DelegateCommand(() => Previous(), () => PreviousCanBeExecuted());
            NextCommand = new DelegateCommand(async () => await NextAsync(), () => NextCanBeExecuted());
            RegisterCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(new Uri((SelectedProvider as WallabagProvider).Url, "/register")),
                () => RegistrationCanBeExecuted());
            WhatIsWallabagCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(new Uri("vimeo://v/167435064"), new LauncherOptions() { FallbackUri = new Uri("https://vimeo.com/167435064") }));
            ScanQRCodeCommand = new DelegateCommand(async () =>
            {
                SystemNavigationManager.GetForCurrentView().BackRequested += QRCodeBackRequested;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

                var rootModalDialog = Window.Current.Content as ModalDialog;
                _oldFrame = rootModalDialog.Content as Frame;

                var scanner = new ZXing.Mobile.MobileBarcodeScanner(CoreWindow.GetForCurrentThread().Dispatcher)
                {
                    RootFrame = new Frame(),
                    TopText = GeneralHelper.LocalizedResource("HoldCameraOntoQRCodeMessage")
                };

                rootModalDialog.Content = scanner.RootFrame;
                var result = await scanner.Scan(new ZXing.Mobile.MobileBarcodeScanningOptions()
                {
                    TryHarder = false,
                    PossibleFormats = new List<ZXing.BarcodeFormat>() { ZXing.BarcodeFormat.QR_CODE }
                });

                if (string.IsNullOrEmpty(result?.Text) == false && result.Text.StartsWith("wallabag://"))
                {
                    SelectedProvider = WallabagProvider.Other;

                    var parsedResult = ProtocolHelper.Parse(result.Text);
                    if (parsedResult.Server.IsValidUri())
                    {
                        Url = parsedResult.Server;
                        Username = parsedResult.Username;
                        CurrentStep = 1;
                    }

                    rootModalDialog.Content = _oldFrame;
                }
            });

            this.PropertyChanged += this_PropertyChanged;
        }

        private void QRCodeBackRequested(object sender, BackRequestedEventArgs args)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= QRCodeBackRequested;
            if (_oldFrame != null)
            {
                args.Handled = true;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                (Window.Current.Content as ModalDialog).Content = _oldFrame;
            }
        }

        private void this_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
                    return;
                }

                CurrentStep += 1;
            }

            if (CurrentStep == 2)
            {
                if (await TestConfigurationAsync())
                    await DownloadAndSaveItemsAndTags();
                else
                {
                    CurrentStep = 1;
                    Messenger.Default.Send(new NotificationMessage(GeneralHelper.LocalizedResource("CredentialsWrongMessage")));
                    return;
                }

                CurrentStep += 1;
                return;
            }

            if (CurrentStep == 3)
            {
                SettingsService.Instance.AccessToken = App.Client.AccessToken;
                SettingsService.Instance.RefreshToken = App.Client.RefreshToken;
                SettingsService.Instance.WallabagUrl = App.Client.InstanceUri;
                SettingsService.Instance.LastTokenRefreshDateTime = App.Client.LastTokenRefreshDateTime;
                SettingsService.Instance.ClientId = App.Client.ClientId;
                SettingsService.Instance.ClientSecret = App.Client.ClientSecret;
                SettingsService.Instance.AllowCollectionOfTelemetryData = (bool)AllowCollectionOfTelemetryData;

                NavigationService.Navigate(typeof(Views.MainPage));
                NavigationService.ClearHistory();

                await TitleBarHelper.ResetAsync();
            }
        }

        private async Task<bool> TestConfigurationAsync()
        {
            ProgressDescription = GeneralHelper.LocalizedResource("TestingConfigurationMessage");

            if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
                Url = "https://" + Url;

            try { await new HttpClient().GetAsync(new Uri(Url)); }
            catch { return false; }

            if (UseCustomSettings == false)
            {
                App.Client.InstanceUri = new Uri(Url);
                var clientCreationIsSuccessful = await CreateApiClientAsync();

                if (clientCreationIsSuccessful == false &&
                    Url.StartsWith("https://"))
                {
                    Url = Url.Replace("https://", "http://");
                    App.Client.InstanceUri = new Uri(Url);

                    if (await CreateApiClientAsync() == false)
                        return false;
                }
            }

            App.Client.ClientId = ClientId;
            App.Client.ClientSecret = ClientSecret;
            App.Client.InstanceUri = new Uri(Url);

            var result = await App.Client.RequestTokenAsync(Username, Password).ContinueWith(x =>
            {
                if (x.Exception == null)
                    return x.Result;
                else
                    return false;
            });

            if (result == false && Url.StartsWith("https://"))
            {
                Url = Url.Replace("https://", "http://");
                App.Client.InstanceUri = new Uri(Url);

                result = await App.Client.RequestTokenAsync(Username, Password).ContinueWith(x =>
                {
                    if (x.Exception == null)
                        return x.Result;
                    else
                        return false;
                });
            }

            return result;
        }
        private async Task DownloadAndSaveItemsAndTags()
        {
            ProgressDescription = GeneralHelper.LocalizedResource("DownloadingItemsTextBlock.Text");
            int itemsPerPage = 100;

            var itemResponse = await App.Client.GetItemsWithEnhancedMetadataAsync(itemsPerPage: itemsPerPage);
            var items = itemResponse.Items as List<WallabagItem>;

            // For users with a lot of items
            if (itemResponse.Pages > 1)
                for (int i = 2; i <= itemResponse.Pages; i++)
                {
                    ProgressDescription = string.Format(GeneralHelper.LocalizedResource("DownloadingItemsWithProgress"), items.Count, itemResponse.TotalNumberOfItems);
                    items.AddRange(await App.Client.GetItemsAsync(itemsPerPage: itemsPerPage, pageNumber: i));
                }

            var tags = await App.Client.GetTagsAsync();

            ProgressDescription = GeneralHelper.LocalizedResource("SavingItemsInDatabaseMessage");

            await Task.Run(() =>
            {
                App.Database.RunInTransaction(() =>
                {
                    foreach (var item in items)
                        App.Database.InsertOrReplace((Item)item);

                    foreach (var tag in tags)
                        App.Database.InsertOrReplace((Tag)tag);
                });
            });
        }

        private void BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            if (PreviousCanBeExecuted())
                Previous();
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += BackRequested;

            CameraIsSupported = (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).Count > 0;

            if (state.Count > 0)
            {
                Username = state[nameof(Username)] as string;
                Password = state[nameof(Password)] as string;
                Url = state[nameof(Url)] as string;
                ClientId = state[nameof(ClientId)] as string;
                ClientSecret = state[nameof(ClientSecret)] as string;

                CurrentStep = (int)state[nameof(CurrentStep)];
                UseCustomSettings = (bool?)state[nameof(UseCustomSettings)];

                _restoredFromPageState = true;
            }

            if (parameter is ProtocolSetupNavigationParameter)
            {
                var np = parameter as ProtocolSetupNavigationParameter;
                Username = np.Username;
                Url = np.Server;

                SelectedProvider = WallabagProvider.Other;

                CurrentStep = 1;
                return;
            }
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= BackRequested;

            if (suspending)
            {
                pageState[nameof(Username)] = Username;
                pageState[nameof(Password)] = Password;
                pageState[nameof(Url)] = Url;
                pageState[nameof(ClientId)] = ClientId;
                pageState[nameof(ClientSecret)] = ClientSecret;

                pageState[nameof(CurrentStep)] = CurrentStep;
                pageState[nameof(UseCustomSettings)] = UseCustomSettings;
            }
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
            ProgressDescription = GeneralHelper.LocalizedResource("CreatingClientMessage");

            string token = string.Empty;
            bool useNewApi = false;
            int step = 1;
            HttpResponseMessage message = null;

            try
            {
                _http = new HttpClient();
                var instanceUri = new Uri(Url);

                // Step 1: Login to get a cookie.
                var loginContent = new HttpStringContent($"_username={System.Net.WebUtility.UrlEncode(Username)}&_password={System.Net.WebUtility.UrlEncode(Password)}&_csrf_token={await GetCsrfTokenAsync()}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                var loginResponse = await _http.PostAsync(new Uri(instanceUri, "/login_check"), loginContent);

                if (!loginResponse.IsSuccessStatusCode)
                    return false;

                // Step 2: Get the client token
                step++;
                var clientCreateUri = new Uri(instanceUri, "/developer/client/create");
                token = await GetStringFromHtmlSequenceAsync(clientCreateUri, m_tokenStartString, m_htmlInputEndString);

                // Step 3: Create the new client
                step++;
                var stringContent = string.Empty;
                useNewApi = (await App.Client.GetVersionNumberAsync()).StartsWith("2.0") == false;

                stringContent = $"client[redirect_uris]={GetRedirectUri(useNewApi)}&client[save]=&client[_token]={token}";

                if (useNewApi)
                    stringContent = $"client[name]={new EasClientDeviceInformation().FriendlyName}&" + stringContent;

                var addContent = new HttpStringContent(stringContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                var addResponse = _http.PostAsync(clientCreateUri, addContent);

                message = await addResponse;

                if (!message.IsSuccessStatusCode)
                    return false;

                var result = ParseResult(await message.Content.ReadAsStringAsync(), useNewApi);
                if (result != null)
                {
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
                Microsoft.HockeyApp.HockeyClient.Current.TrackException(e, new Dictionary<string, string>()
                {
                    { nameof(token), token },
                    { nameof(useNewApi), useNewApi.ToString() },
                    { nameof(step), step.ToString() },
                    { "StatusCode",  message?.StatusCode.ToString() },
                    { "IsSuccessStatusCode",  await message?.Content?.ReadAsStringAsync() },
                    { nameof(Url), Url?.ToString() }
                });
                return false;
            }
        }

        private object GetRedirectUri(bool useNewApi) => useNewApi ? default(Uri) : new Uri(new Uri(Url), new EasClientDeviceInformation().FriendlyName);
        private Task<string> GetCsrfTokenAsync() => GetStringFromHtmlSequenceAsync(new Uri(new Uri(Url), "/login"), m_loginStartString, m_htmlInputEndString);

        private async Task<string> GetStringFromHtmlSequenceAsync(Uri uri, string startString, string endString)
        {
            string html = await (await _http.GetAsync(uri)).Content.ReadAsStringAsync();

            int startIndex = html.IndexOf(startString) + startString.Length;
            int endIndex = html.IndexOf(endString, startIndex);

            return html.Substring(startIndex, endIndex - startIndex);
        }

        private ClientResultData ParseResult(string html, bool useNewApi = false)
        {
            try
            {
                var results = new List<string>();

                var lastIndex = 0;
                int resultCount = useNewApi ? 2 : 1;
                do
                {
                    var start = html.IndexOf(m_finalTokenStartString, lastIndex) + m_finalTokenStartString.Length;
                    lastIndex = html.IndexOf(m_finalTokenEndString, start);

                    results.Add(html.Substring(start, lastIndex - start));

                } while (results.Count <= resultCount);

                if (useNewApi)
                    return new ClientResultData()
                    {
                        Name = results[0],
                        Id = results[1],
                        Secret = results[2]
                    };
                else
                    return new ClientResultData()
                    {
                        Id = results[0],
                        Secret = results[1],
                        Name = string.Empty
                    };
            }
            catch (ArgumentOutOfRangeException exception)
            {
                var exceptionMetadata = new Dictionary<string, string>
                {
                    { nameof(useNewApi), useNewApi.ToString() },
                    { "HTML", html },
                    { nameof(Url), Url }
                };
                Microsoft.HockeyApp.HockeyClient.Current.TrackException(exception, exceptionMetadata);
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

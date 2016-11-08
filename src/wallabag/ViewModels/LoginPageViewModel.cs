using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Api.Models;
using wallabag.Common.Helpers;
using wallabag.Models;
using wallabag.Services;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class LoginPageViewModel : ViewModelBase
    {
        private bool _credentialsAreExisting = false;
        private bool _restoredFromPageState = false;

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
        public Visibility UrlFieldVisibility { get { return (_credentialsAreExisting == false && (SelectedProvider as WallabagProvider)?.Url == null) ? Visibility.Visible : Visibility.Collapsed; } }

        public List<WallabagProvider> Providers { get; set; }
        public object SelectedProvider { get; set; }

        public DelegateCommand PreviousCommand { get; private set; }
        public DelegateCommand NextCommand { get; private set; }
        public DelegateCommand RegisterCommand { get; private set; }
        public DelegateCommand WhatIsWallabagCommand { get; private set; }
        public Task TitleBarExtensions { get; private set; }

        public LoginPageViewModel()
        {
            Providers = new List<WallabagProvider>()
            {
                //new WallabagProvider(new Uri("https://framabag.org"), "framabag", GeneralHelper.LocalizedResource("FramabagProviderDescription")),
                new WallabagProvider(new Uri("http://v2.wallabag.org"), "v2.wallabag.org", GeneralHelper.LocalizedResource("V2WallabagOrgProviderDescription")),
                WallabagProvider.Other
            };

            PreviousCommand = new DelegateCommand(() => Previous(), () => PreviousCanBeExecuted());
            NextCommand = new DelegateCommand(async () => await NextAsync(), () => NextCanBeExecuted());
            RegisterCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(new Uri((SelectedProvider as WallabagProvider).Url, "/register")),
                () => RegistrationCanBeExecuted());
            WhatIsWallabagCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(new Uri("vimeo://v/167435064"), new LauncherOptions() { FallbackUri = new Uri("https://vimeo.com/167435064") }));

            this.PropertyChanged += this_PropertyChanged;
        }

        private void this_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PreviousCommand.RaiseCanExecuteChanged();
            NextCommand.RaiseCanExecuteChanged();
            RegisterCommand.RaiseCanExecuteChanged();
        }

        private bool PreviousCanBeExecuted()
        {
            if (_credentialsAreExisting)
                return false;
            else
                return CurrentStep > 0 &&
                       CurrentStep != 2;
        }
        private bool NextCanBeExecuted()
        {
            return ((_credentialsAreExisting || SelectedProvider != null) || _restoredFromPageState) &&
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
            else if (CurrentStep == 3 && _credentialsAreExisting == false)
                CurrentStep = 1;
        }
        private async Task NextAsync()
        {
            if (_credentialsAreExisting && CurrentStep == 0)
                CurrentStep = 2;

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

            if (UseCustomSettings == false && _credentialsAreExisting == false)
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

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            _credentialsAreExisting = parameter != null && parameter is bool && (bool)parameter;

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

            if (_credentialsAreExisting)
            {
                UseCustomSettings = true;
                ClientId = App.Client.ClientId;
                ClientSecret = App.Client.ClientSecret;
                SelectedProvider = new WallabagProvider(App.Client.InstanceUri, string.Empty);
                Url = App.Client.InstanceUri.ToString();

                await NextAsync();
                return;
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

            _http = new HttpClient();
            var instanceUri = new Uri(Url);

            // Step 1: Login to get a cookie.
            var loginContent = new HttpStringContent($"_username={Username}&_password={Password}&_csrf_token={await GetCsrfTokenAsync()}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
            var loginResponse = await _http.PostAsync(new Uri(instanceUri, "/login_check"), loginContent);

            if (!loginResponse.IsSuccessStatusCode)
                return false;

            // Step 2: Get the client token
            var clientCreateUri = new Uri(instanceUri, "/developer/client/create");
            var token = await GetStringFromHtmlSequenceAsync(clientCreateUri, m_tokenStartString, m_htmlInputEndString);

            // Step 3: Create the new client

            var stringContent = string.Empty;
            var useNewApi = (await App.Client.GetVersionNumberAsync()).StartsWith("2.0") == false;

            stringContent = $"client[redirect_uris]={GetRedirectUri(useNewApi)}&client[save]=&client[_token]={token}";

            if (useNewApi)
                stringContent = $"client[name]={new EasClientDeviceInformation().FriendlyName}&" + stringContent;

            var addContent = new HttpStringContent(stringContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
            var addResponse = await _http.PostAsync(clientCreateUri, addContent);

            if (!addResponse.IsSuccessStatusCode)
                return false;

            var result = ParseResult(await addResponse.Content.ReadAsStringAsync(), useNewApi);

            ClientId = result.Id;
            ClientSecret = result.Secret;

            _http.Dispose();
            return true;
        }

        private object GetRedirectUri(bool useNewApi) => useNewApi ? default(Uri) : new Uri(new Uri(Url), new EasClientDeviceInformation().FriendlyName);
        private Task<string> GetCsrfTokenAsync() => GetStringFromHtmlSequenceAsync(new Uri(new Uri(Url), "/login"), m_loginStartString, m_htmlInputEndString);

        private async Task<string> GetStringFromHtmlSequenceAsync(Uri uri, string startString, string endString)
        {
            var html = await (await _http.GetAsync(uri)).Content.ReadAsStringAsync();

            var startIndex = html.IndexOf(startString) + startString.Length;
            var endIndex = html.IndexOf(endString, startIndex);

            return html.Substring(startIndex, endIndex - startIndex);
        }

        private ClientResultData ParseResult(string html, bool useNewApi = false)
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

        #endregion
    }

    public class ClientResultData
    {
        public string Id { get; set; }
        public string Secret { get; set; }
        public string Name { get; set; }
    }
}

using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Api.Models;
using wallabag.Common;
using wallabag.Common.Messages;
using wallabag.Models;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class LoginPageViewModel : ViewModelBase
    {
        public bool CredentialsAreExisting { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Url { get; set; } = "https://";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public bool? UseCustomSettings { get; set; } = false;

        public int CurrentStep { get; set; } = 0;

        public List<WallabagProvider> Providers { get; set; }
        public object SelectedProvider { get; set; }

        public DelegateCommand PreviousCommand { get; private set; }
        public DelegateCommand NextCommand { get; private set; }

        public LoginPageViewModel()
        {
            Providers = new List<WallabagProvider>()
            {
                new WallabagProvider(new Uri("https://framabag.org"), "framabag", "Still not on wallabag 2.x, sorry. :("),
                new WallabagProvider(new Uri("http://v2.wallabag.org"), "v2.wallabag.org", "Always testing the latest beta versions?"),
                new WallabagProvider(default(Uri), "other", "If you're using another provider, this option is for you.")
            };

            PreviousCommand = new DelegateCommand(async () => await PreviousAsync(), () => CurrentStep > 0);
            NextCommand = new DelegateCommand(async () => await NextAsync(), () => NextCanBeExecuted());

            this.PropertyChanged += this_PropertyChanged;
        }

        private void this_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PreviousCommand.RaiseCanExecuteChanged();
            NextCommand.RaiseCanExecuteChanged();
        }

        private bool NextCanBeExecuted()
        {
            return SelectedProvider != null &&
                   CurrentStep <= 3;
        }

        private async Task PreviousAsync()
        {
            PreviousCommand.RaiseCanExecuteChanged();
            NextCommand.RaiseCanExecuteChanged();

            CurrentStep -= 1;
        }
        private async Task NextAsync()
        {
            PreviousCommand.RaiseCanExecuteChanged();
            NextCommand.RaiseCanExecuteChanged();

            var selectedProvider = SelectedProvider as WallabagProvider;

            if (CurrentStep == 0)
            {
                if (selectedProvider.Url == null)
                {
                    Messenger.Default.Send(new ShowUrlFieldMessage());
                    Url = "https://";
                }
                else
                    Url = selectedProvider.Url.ToString();
            }

            CurrentStep += 1;
        }

        private async Task<bool> TestConfigurationAsync()
        {
            if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
                Url = "https://" + Url;

            if (UseCustomSettings == false)
            {
                var clientCreationIsSuccessful = await CreateApiClientAsync();

                if (clientCreationIsSuccessful == false &&
                    Url.StartsWith("https://"))
                {
                    Url = Url.Replace("https://", "http://");

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
        private async Task ContinueAsync(bool credentialsExist = false)
        {
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

            await Task.Run(() =>
            {
                App.Database.InsertOrReplaceAll(items, typeof(Item));
                App.Database.InsertOrReplaceAll(tags, typeof(Tag));
            });

            await TitleBarExtensions.ResetAsync();

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

            if (state.Count == 5)
            {
                Username = state[nameof(Username)] as string;
                Password = state[nameof(Password)] as string;
                Url = state[nameof(Url)] as string;
                ClientId = state[nameof(ClientId)] as string;
                ClientSecret = state[nameof(ClientSecret)] as string;
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
            var useNewApi = (await App.Client.GetVersionNumberAsync()).StartsWith("2.1"); // TODO: Needs a fix in wallabag-api because the credentials aren't necessary.

            stringContent = $"client[redirect_uris]={GetRedirectUri(useNewApi)}&client[save]=&client[_token]={token}";

            if (useNewApi)
                stringContent = $"client[name]={new EasClientDeviceInformation().FriendlyName}&" + stringContent;

            var addContent = new HttpStringContent(stringContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
            var addResponse = await _http.PostAsync(clientCreateUri, addContent);

            if (!addResponse.IsSuccessStatusCode)
                return false;

            var results = ParseResult(await addResponse.Content.ReadAsStringAsync());

            if (results.Count == 2)
                this.ClientId = results[0];
            else
                this.ClientId = results[1];
            this.ClientSecret = results[2];

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

        private List<string> ParseResult(string html)
        {
            var results = new List<string>();

            var lastIndex = 0;
            do
            {
                var start = html.IndexOf(m_finalTokenStartString, lastIndex) + m_finalTokenStartString.Length;
                lastIndex = html.IndexOf(m_finalTokenEndString, start);

                results.Add(html.Substring(start, lastIndex - start));

            } while (results.Count <= 3);

            return results;
        }

        #endregion
    }
}

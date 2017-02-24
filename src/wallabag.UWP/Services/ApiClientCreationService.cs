using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using Windows.Web.Http;

namespace wallabag.Services
{
    public class ApiClientCreationService : IApiClientCreationService
    {
        private readonly ILoggingService _loggingService;
        private readonly IWallabagClient _client;
        private readonly IPlatformSpecific _device;

        private HttpClient _http;

        private const string m_LOGINSTARTSTRING = "<input type=\"hidden\" name=\"_csrf_token\" value=\"";
        private const string m_TOKENSTARTSTRING = "<input type=\"hidden\" id=\"client__token\" name=\"client[_token]\" value=\"";
        private const string m_FINALTOKENSTARTSTRING = "<strong><pre>";
        private const string m_FINALTOKENENDSTRING = "</pre></strong>";
        private const string m_HTMLINPUTENDSTRING = "\" />";

        public ApiClientCreationService(
            ILoggingService loggingService,
            IWallabagClient client,
            IPlatformSpecific platform)
        {
            _loggingService = loggingService;
            _client = client;
            _device = platform;
        }

        public async Task<ClientCreationData> CreateClientAsync(string Url, string Username, string Password)
        {
            _loggingService.WriteLine("Creating a new client...");

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
                var loginContent = new HttpStringContent($"_username={System.Net.WebUtility.UrlEncode(Username)}&_password={System.Net.WebUtility.UrlEncode(Password)}&_csrf_token={await GetCsrfTokenAsync(instanceUri)}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                var loginResponse = await _http.PostAsync(instanceUri.Append("/login_check"), loginContent);

                // TODO: Apparently the HttpClient doesn't handle cookies properly. Find a workaround for this issue.
                // In the meantime, an UWP implementation based on the Windows.Web.HttpClient is used.

                if (!loginResponse.IsSuccessStatusCode)
                {
                    _loggingService.WriteLine($"Failed. Resulted content: {await loginResponse.Content.ReadAsStringAsync()}", LoggingCategory.Warning);
                    return new ClientCreationData();
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

                stringContent = $"client[redirect_uris]={GetRedirectUri(instanceUri, useNewApi)}&client[save]=&client[_token]={token}";

                if (useNewApi)
                    stringContent = $"client[name]={_device.DeviceName}&" + stringContent;

                _loggingService.WriteLine($"Content: {stringContent}");

                var addContent = new HttpStringContent(stringContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                var addResponse = _http.PostAsync(clientCreateUri, addContent);

                message = await addResponse;

                if (!message.IsSuccessStatusCode)
                {
                    _loggingService.WriteLine($"Failed. Resulted content: {await message.Content.ReadAsStringAsync()}", LoggingCategory.Warning);
                    return new ClientCreationData();
                }

                string content = await message.Content.ReadAsStringAsync();
                _loggingService.WriteLine($"Parsing the resulted string: {content}");

                var result = ParseResult(content, useNewApi) ?? new ClientCreationData();
                _loggingService.WriteLineIf(result.Success, "Success!");

                _http.Dispose();
                return result;
            }
            catch (Exception e)
            {
                _loggingService.TrackException(e);
                return new ClientCreationData();
            }
        }

        public Uri GetRedirectUri(Uri baseUri, bool useNewApi) => useNewApi ? default(Uri) : baseUri.Append(_device.DeviceName);
        private Task<string> GetCsrfTokenAsync(Uri baseUri) => GetStringFromHtmlSequenceAsync(baseUri.Append("/login"), m_LOGINSTARTSTRING, m_HTMLINPUTENDSTRING);

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

        private ClientCreationData ParseResult(string html, bool useNewApi = false)
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

                var finalResult = default(ClientCreationData);
                if (useNewApi)
                    finalResult = new ClientCreationData()
                    {
                        Name = results[0],
                        Id = results[1],
                        Secret = results[2]
                    };
                else
                    finalResult = new ClientCreationData()
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
                return null;
            }
        }
    }
}

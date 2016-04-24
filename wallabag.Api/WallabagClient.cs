using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace wallabag.Api
{
    public partial class WallabagClient : IWallabagClient
    {
        private HttpClient _httpClient;

        public WallabagClient(Uri Uri, string ClientId, string ClientSecret)
        {
            this.InstanceUri = Uri;
            this.ClientId = ClientId;
            this.ClientSecret = ClientSecret;

            this._httpClient = new HttpClient();          
        }

        protected async Task<HttpResponseMessage> ExecuteHttpRequestAsync(HttpRequestMethod httpRequestMethod, string RelativeUriString, Dictionary<string, object> parameters = default(Dictionary<string, object>))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", await GetAccessTokenAsync());

            if (string.IsNullOrEmpty(AccessToken))
                throw new Exception("Access token not available. Please create one using the GetAccessTokenAsync() method first.");

            Uri requestUri = new Uri($"{InstanceUri.ToString()}/api{RelativeUriString}.json");
            var content = new HttpStringContent(JsonConvert.SerializeObject(parameters), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");

            string httpMethodString = "GET";
            switch (httpRequestMethod)
            {
                case HttpRequestMethod.Delete: httpMethodString = "DELETE"; break;
                case HttpRequestMethod.Patch: httpMethodString = "PATCH"; break;
                case HttpRequestMethod.Post: httpMethodString = "POST"; break;
                case HttpRequestMethod.Put: httpMethodString = "PUT"; break;
            }

            var method = new HttpMethod(httpMethodString);
            var request = new HttpRequestMessage(method, requestUri);

            if (parameters != null)
                request.Content = content;

            try { return await _httpClient.SendRequestAsync(request); }
            catch (Exception ex) { throw ex; }
        }
        public enum HttpRequestMethod { Delete, Get, Patch, Post, Put }

    }
}

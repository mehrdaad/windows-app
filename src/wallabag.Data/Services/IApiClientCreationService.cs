using System;
using System.Threading.Tasks;
using wallabag.Data.Models;

namespace wallabag.Data.Services
{
    public interface IApiClientCreationService
    {
        Task<ClientCreationData> CreateClientAsync(string url, string username, string password);
        Uri GetRedirectUri(Uri baseUri, bool useNewApi);
    }
}

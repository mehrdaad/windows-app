using System;
using System.Threading.Tasks;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
        public Uri InstanceUri { get; set; }

        protected DateTime _LastRequestDateTime;

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public Task<string> GetAccessTokenAsync()
        {
            throw new NotImplementedException();
        }
        public Task<string> RefreshAccessTokenAsync()
        {
            throw new NotImplementedException();
        }
    }
}

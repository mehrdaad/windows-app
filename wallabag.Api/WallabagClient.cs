using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public class WallabagClient : IWallabagClient
    {
        public Uri InstanceUri { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public Task<string> GetAccessTokenAsync() { throw new NotImplementedException(); }
        public Task<string> RefreshAccessTokenAsync() { throw new NotImplementedException(); }

        public Task<WallabagItem> AddAsync(Uri uri, string[] tags = null, string title = null) { throw new NotImplementedException(); }

        public Task<IEnumerable<WallabagItem>> GetItemsAsync(
            // TODO: Add item properties.
            )
        { throw new NotImplementedException(); }
        public Task<WallabagItem> GetItemAsync(string itemId) { throw new NotImplementedException(); }
        public Task<WallabagTag> GetTagsAsync() { throw new NotImplementedException(); }

        public Task<bool> ArchiveAsync(string itemId) { throw new NotImplementedException(); }
        public Task<bool> ArchiveAsync(WallabagItem item) { throw new NotImplementedException(); }
        public Task<bool> UnarchiveAsync(string itemId) { throw new NotImplementedException(); }
        public Task<bool> UnarchiveAsync(WallabagItem item) { throw new NotImplementedException(); }
        public Task<bool> FavoriteAsync(string itemId) { throw new NotImplementedException(); }
        public Task<bool> FavoriteAsync(WallabagItem item) { throw new NotImplementedException(); }
        public Task<bool> UnfavoriteAsync(string itemId) { throw new NotImplementedException(); }
        public Task<bool> UnfavoriteAsync(WallabagItem item) { throw new NotImplementedException(); }
        public Task<bool> DeleteAsync(string itemId) { throw new NotImplementedException(); }
        public Task<bool> DeleteAsync(WallabagItem item) { throw new NotImplementedException(); }

        public Task<bool> AddTagsAsync(string itemId, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> AddTagsAsync(WallabagItem item, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> RemoveTagsAsync(string itemId, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> RemoveTagsAsync(WallabagItem item, string[] tags) { throw new NotImplementedException(); }
    }
}

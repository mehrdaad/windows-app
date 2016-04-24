using System;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
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
    }
}

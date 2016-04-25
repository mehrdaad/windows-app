using System;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
        public Task<bool> ArchiveAsync(int itemId) { throw new NotImplementedException(); }
        public Task<bool> ArchiveAsync(WallabagItem item) { throw new NotImplementedException(); }
        public Task<bool> UnarchiveAsync(int itemId) { throw new NotImplementedException(); }
        public Task<bool> UnarchiveAsync(WallabagItem item) { throw new NotImplementedException(); }
        public Task<bool> FavoriteAsync(int itemId) { throw new NotImplementedException(); }
        public Task<bool> FavoriteAsync(WallabagItem item) { throw new NotImplementedException(); }
        public Task<bool> UnfavoriteAsync(int itemId) { throw new NotImplementedException(); }
        public Task<bool> UnfavoriteAsync(WallabagItem item) { throw new NotImplementedException(); }
        public Task<bool> DeleteAsync(int itemId) { throw new NotImplementedException(); }
        public Task<bool> DeleteAsync(WallabagItem item) { throw new NotImplementedException(); }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
        public async Task<bool> ArchiveAsync(int itemId)
        {
            if (itemId == 0)
                throw new ArgumentNullException(nameof(itemId));

            var jsonString = await ExecuteHttpRequestAsync(HttpRequestMethod.Patch, $"/entries/{itemId}", new Dictionary<string, object>() { ["archive"] = true.ToInt() });
            var item = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<WallabagItem>(jsonString));

            return item.IsRead == true;
        }
        public Task<bool> ArchiveAsync(WallabagItem item) { return ArchiveAsync(item.Id); }
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

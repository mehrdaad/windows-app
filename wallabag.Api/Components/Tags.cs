using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
        public async Task<IEnumerable<WallabagTag>> GetTagsAsync()
        {
            var jsonString = await ExecuteHttpRequestAsync(HttpRequestMethod.Get, "/tags");
            return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<IEnumerable<WallabagTag>>(jsonString));
        }

        public Task<bool> AddTagsAsync(int itemId, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> AddTagsAsync(WallabagItem item, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> RemoveTagsAsync(int itemId, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> RemoveTagsAsync(WallabagItem item, string[] tags) { throw new NotImplementedException(); }
    }
}

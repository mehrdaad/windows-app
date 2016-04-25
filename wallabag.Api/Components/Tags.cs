using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
        public Task<IEnumerable<WallabagTag>> GetTagsAsync() { throw new NotImplementedException(); }

        public Task<bool> AddTagsAsync(int itemId, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> AddTagsAsync(WallabagItem item, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> RemoveTagsAsync(int itemId, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> RemoveTagsAsync(WallabagItem item, string[] tags) { throw new NotImplementedException(); }
    }
}

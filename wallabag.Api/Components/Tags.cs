using System;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
        public Task<WallabagTag> GetTagsAsync() { throw new NotImplementedException(); }

        public Task<bool> AddTagsAsync(string itemId, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> AddTagsAsync(WallabagItem item, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> RemoveTagsAsync(string itemId, string[] tags) { throw new NotImplementedException(); }
        public Task<bool> RemoveTagsAsync(WallabagItem item, string[] tags) { throw new NotImplementedException(); }
    }
}

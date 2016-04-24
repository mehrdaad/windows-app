using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
        public Task<IEnumerable<WallabagItem>> GetItemsAsync(/* TODO: Item properties */)
        {
            throw new NotImplementedException();
        }
        public Task<WallabagItem> GetItemAsync(string itemId)
        {
            throw new NotImplementedException();
        }
    }
}

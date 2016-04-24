using System;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
        public Task<WallabagItem> AddAsync(Uri uri, string[] tags = null, string title = null)
        {
            throw new NotImplementedException();
        }
    }
}

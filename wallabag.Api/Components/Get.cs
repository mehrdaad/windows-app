using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api.Models;

namespace wallabag.Api
{
    public partial class WallabagClient
    {
        public Task<IEnumerable<WallabagItem>> GetItemsAsync(
            bool? IsRead = null,
            bool? IsStarred = null,
            WallabagDateOrder? DateOrder = null,
            WallabagSortOrder? SortOrder = null,
            int? PageNumber = null,
            int? ItemsPerPage = null,
            string[] Tags = null)
        {
            throw new NotImplementedException();
        }
        public Task<WallabagItem> GetItemAsync(string itemId)
        {
            throw new NotImplementedException();
        }

        public enum WallabagDateOrder { ByCreationDate, ByLastModificationDate }
        public enum WallabagSortOrder { Ascending, Descending }
    }
}

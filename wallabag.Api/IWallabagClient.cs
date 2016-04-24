using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api.Models;
using static wallabag.Api.WallabagClient;

namespace wallabag.Api
{
    interface IWallabagClient
    {
        Uri InstanceUri { get; set; }
        string ClientId { get; set; }
        string ClientSecret { get; set; }
        string AccessToken { get; set; }
        string RefreshToken { get; set; }

        Task<string> GetAccessTokenAsync();
        Task<bool> RefreshAccessTokenAsync();
        Task<bool> RequestTokenAsync(string username, string password);

        Task<WallabagItem> AddAsync(Uri uri, string[] tags = null, string title = null);

        Task<IEnumerable<WallabagItem>> GetItemsAsync(
            bool? IsRead = null,
            bool? IsStarred = null,
            WallabagDateOrder? DateOrder = null,
            WallabagSortOrder? SortOrder = null,
            int? PageNumber = null,
            int? ItemsPerPage = null,
            string[] Tags = null);
        Task<WallabagItem> GetItemAsync(string itemId);
        Task<WallabagTag> GetTagsAsync();

        Task<bool> ArchiveAsync(string itemId);
        Task<bool> ArchiveAsync(WallabagItem item);
        Task<bool> UnarchiveAsync(string itemId);
        Task<bool> UnarchiveAsync(WallabagItem item);
        Task<bool> FavoriteAsync(string itemId);
        Task<bool> FavoriteAsync(WallabagItem item);
        Task<bool> UnfavoriteAsync(string itemId);
        Task<bool> UnfavoriteAsync(WallabagItem item);
        Task<bool> DeleteAsync(string itemId);
        Task<bool> DeleteAsync(WallabagItem item);

        Task<bool> AddTagsAsync(string itemId, string[] tags);
        Task<bool> AddTagsAsync(WallabagItem item, string[] tags);
        Task<bool> RemoveTagsAsync(string itemId, string[] tags);
        Task<bool> RemoveTagsAsync(WallabagItem item, string[] tags);
    }
}

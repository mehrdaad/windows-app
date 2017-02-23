using System;
using System.Threading.Tasks;
using SQLite.Net.Interop;
using wallabag.Data.Models;

namespace wallabag.Data.Interfaces
{
    public interface IPlatformSpecific
    {
        Task<bool> GetHasCameraAsync();

        string DeviceName { get; }
        bool InternetConnectionIsAvailable { get; }
        string AccentColorHexCode { get; }
        Uri RateAppUri { get; }
        string AppVersion { get; }

        void LaunchUri(Uri uri, Uri fallback = default(Uri));
        string GetLocalizedResource(string resourceName);
        Task RunOnUIThreadAsync(Action p);
        Task<string> GetArticleTemplateAsync();
        void ShareItem(Item model);
        void CloseApplication();

        Task<string> GetDatabasePathAsync();
        Task DeleteDatabaseAsync();
        ISQLitePlatform GetSQLitePlatform();
    }
}

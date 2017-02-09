using System;
using System.Threading.Tasks;
using SQLite.Net.Interop;
using wallabag.Data.Models;

namespace wallabag.Data.Interfaces
{
    interface IPlatformSpecific
    {
        bool HasACamera { get; }
        string DeviceName { get; }
        bool InternetConnectionIsAvailable { get; }
        string AccentColorHexCode { get; }
        Uri RateAppUri { get; }
        string AppVersion { get; set; }
        string DatabasePath { get; set; }

        void LaunchUri(Uri uri, Uri fallback = default(Uri));
        string GetLocalizedResource(string resourceName);
        Task RunOnUIThreadAsync(Action p);
        Task<string> GetArticleTemplateAsync();
        void ShareItem(Item model);
        void CloseApplication();
        void DeleteDatabase();
        ISQLitePlatform GetSQLitePlatform();
    }
}

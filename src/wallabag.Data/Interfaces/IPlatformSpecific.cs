using System;
using System.Threading.Tasks;
using wallabag.Data.Models;

namespace wallabag.Data.Interfaces
{
    interface IPlatformSpecific
    {
        bool HasACamera { get; }
        string DeviceName { get; }
        bool InternetConnectionIsAvailable { get; }
        string AccentColorHexCode { get; }

        void LaunchUri(Uri uri, Uri fallback = default(Uri));
        string GetLocalizedResource(string resourceName);
        Task RunOnUIThreadAsync(Action p);
        Task<string> GetArticleTemplateAsync();
        void ShareItem(Item model);
    }
}

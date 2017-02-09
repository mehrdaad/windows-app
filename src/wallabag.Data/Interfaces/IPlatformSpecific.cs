using System;
using System.Threading.Tasks;

namespace wallabag.Data.Interfaces
{
    interface IPlatformSpecific
    {
        bool HasACamera { get; }
        string DeviceName { get; }

        void LaunchUri(Uri uri, Uri fallback = default(Uri));
        Task RunOnUIThreadAsync(Action p);
    }
}

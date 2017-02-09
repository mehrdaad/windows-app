using System;

namespace wallabag.Data.Interfaces
{
    interface IPlatformSpecific
    {
        bool HasACamera { get; }
        string DeviceName { get; }

        void LaunchUri(Uri uri, Uri fallback = default(Uri));
    }
}

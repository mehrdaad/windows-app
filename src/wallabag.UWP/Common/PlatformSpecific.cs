using SQLite.Net.Interop;
using System;
using System.Threading.Tasks;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace wallabag.Common
{
    class PlatformSpecific : IPlatformSpecific
    {
        public async Task<bool> GetHasCameraAsync()
            => (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).Count > 0;

        public string DeviceName => new EasClientDeviceInformation().FriendlyName;

        public bool InternetConnectionIsAvailable
        {
            get
            {
                var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
                return profile?.GetNetworkConnectivityLevel() == Windows.Networking.Connectivity.NetworkConnectivityLevel.InternetAccess;
            }
        }

        public string AccentColorHexCode => ((Color)Application.Current.Resources["SystemAccentColor"]).ToString();

        public Uri RateAppUri => new Uri("ms-windows-store://review/?ProductId=" + Package.Current.Id.FamilyName);

        public string AppVersion
        {
            get
            {
                var package = Package.Current;
                var packageId = package.Id;
                var version = packageId.Version;

                return string.Format($"{version.Major}.{version.Minor}.{version.Build}");
            }
        }

        public async Task<string> GetDatabasePathAsync()
            => (await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("wallabag.db", CreationCollisionOption.OpenIfExists)).Path;

        public void CloseApplication() => Application.Current.Exit();

        public async Task DeleteDatabaseAsync()
        {
            var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("wallabag.db", CreationCollisionOption.OpenIfExists);
            await file.DeleteAsync();
        }

        public async Task<string> GetArticleTemplateAsync()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Article/article.html"));
            return await FileIO.ReadTextAsync(file);
        }

        public string GetLocalizedResource(string resourceName)
            => ResourceLoader.GetForCurrentView().GetString(resourceName.Replace(".", "/"));

        public ISQLitePlatform GetSQLitePlatform() => new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT();

        public void LaunchUri(Uri uri, Uri fallback = null)
            => Launcher.LaunchUriAsync(uri, new LauncherOptions() { FallbackUri = fallback }).AsTask().ConfigureAwait(false);

        public Task RunOnUIThreadAsync(Action p)
            => CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => p.Invoke()).AsTask();

        public void ShareItem(Item model)
        {
            DataTransferManager.GetForCurrentView().DataRequested += (s, e) =>
            {
                e.Request.Data.SetWebLink(new Uri(model.Url));
                e.Request.Data.Properties.Title = model.Title;
            };
            DataTransferManager.ShowShareUI();
        }
    }
}

﻿using GalaSoft.MvvmLight.Ioc;
using SQLite.Net;
using SQLite.Net.Interop;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
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

        public string GetDatabasePath()
            => ApplicationData.Current.LocalCacheFolder.CreateFileAsync("wallabag.db", CreationCollisionOption.OpenIfExists).AsTask().Result.Path;

        public void CloseApplication() => Application.Current.Exit();

        public async Task DeleteDatabaseAsync(SQLiteConnection connection)
        {
            var file = await StorageFile.GetFileFromPathAsync(connection.DatabasePath);
            await file.DeleteAsync();

            CloseApplication();
        }

        public async Task<string> GetArticleTemplateAsync()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Article/article.html"));
            string content = await FileIO.ReadTextAsync(file);

            var javaScriptRegex = new Regex("<script src=\"(?<uri>ms-appx-web://[\\w./-]*(?!.js))\" type=\"text/javascript\"></script>");
            var cssRegex = new Regex("<link rel=\"stylesheet\" href=\"(?<uri>ms-appx-web://[\\w./-]*(?!.css))\" type=\"text/css\" media=\"screen\" />");

            var jsMatches = javaScriptRegex.Matches(content);
            foreach (Match match in jsMatches)
            {
                string uri = match.Groups["uri"].Value.Replace("appx-web", "appx");
                var replacementFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

                content = content.Replace(match.Value, $"<script>{await FileIO.ReadTextAsync(replacementFile)}</script>");
            }

            var cssMatches = cssRegex.Matches(content);
            foreach (Match match in cssMatches)
            {
                string uri = match.Groups["uri"].Value.Replace("appx-web", "appx");
                var replacementFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

                content = content.Replace(match.Value, $"<style>{await FileIO.ReadTextAsync(replacementFile)}</style>");
            }

            return content;
        }

        public string GetLocalizedResource(string resourceName)
            => ResourceLoader.GetForCurrentView().GetString(resourceName.Replace(".", "/"));

        public ISQLitePlatform GetSQLitePlatform() => new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT();

        public void LaunchUri(Uri uri, Uri fallback = null)
            => Launcher.LaunchUriAsync(uri, new LauncherOptions() { FallbackUri = fallback }).AsTask().ConfigureAwait(false);

        public Task RunOnUIThreadAsync(Action p)
            => CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => p.Invoke()).AsTask();

        public void ShareItem(Item model)
        {
            DataTransferManager.GetForCurrentView().DataRequested += (s, e) =>
            {
                e.Request.Data.SetWebLink(new Uri(model.Url));
                e.Request.Data.SetText(model.Title);
                e.Request.Data.Properties.Title = model.Title;
            };
            DataTransferManager.ShowShareUI();
        }

        public void SetClipboardUri(Uri rightClickUri)
        {
            SimpleIoc.Default.GetInstance<ILoggingService>().WriteLine($"Set clipboard uri to: {rightClickUri}");

            var datapackage = new DataPackage()
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            datapackage.SetWebLink(rightClickUri);
            datapackage.SetText(rightClickUri.OriginalString);

            Clipboard.SetContent(datapackage);
            Clipboard.Flush();
        }
    }
}

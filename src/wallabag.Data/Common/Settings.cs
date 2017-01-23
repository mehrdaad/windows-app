using GalaSoft.MvvmLight.Ioc;
using System;
using wallabag.Data.Services;

namespace wallabag.Data.Common
{
    public class Settings
    {
        public class Authentication
        {
            public static Uri WallabagUri
            {
                get { return SettingsService.GetValueOrDefault(nameof(WallabagUri), default(Uri)); }
                set { SettingsService.AddOrUpdateValue(nameof(WallabagUri), value); }
            }
            public static string ClientId
            {
                get { return SettingsService.GetValueOrDefault(nameof(ClientId), default(string)); }
                set { SettingsService.AddOrUpdateValue(nameof(ClientId), value); }
            }
            public static string ClientSecret
            {
                get { return SettingsService.GetValueOrDefault(nameof(ClientSecret), default(string)); }
                set { SettingsService.AddOrUpdateValue(nameof(ClientSecret), value); }
            }
            public static string AccessToken
            {
                get { return SettingsService.GetValueOrDefault(nameof(AccessToken), default(string)); }
                set { SettingsService.AddOrUpdateValue(nameof(AccessToken), value); }
            }
            public static string RefreshToken
            {
                get { return SettingsService.GetValueOrDefault(nameof(RefreshToken), default(string)); }
                set { SettingsService.AddOrUpdateValue(nameof(RefreshToken), value); }
            }
            public static DateTime LastTokenRefreshDateTime
            {
                get { return SettingsService.GetValueOrDefault(nameof(LastTokenRefreshDateTime), DateTime.Now - TimeSpan.FromHours(1)); }
                set { SettingsService.AddOrUpdateValue(nameof(LastTokenRefreshDateTime), value); }
            }
        }

        public class General
        {
            public static bool SyncOnStartup
            {
                get { return SettingsService.GetValueOrDefault(nameof(SyncOnStartup), true); }
                set { SettingsService.AddOrUpdateValue(nameof(SyncOnStartup), value); }
            }
            public static bool AllowCollectionOfTelemetryData
            {
                get { return SettingsService.GetValueOrDefault(nameof(AllowCollectionOfTelemetryData), true); }
                set { SettingsService.AddOrUpdateValue(nameof(AllowCollectionOfTelemetryData), value); }
            }
            public static DateTime LastSuccessfulSyncDateTime
            {
                get { return SettingsService.GetValueOrDefault(nameof(LastSuccessfulSyncDateTime), default(DateTime)); }
                set { SettingsService.AddOrUpdateValue(nameof(LastSuccessfulSyncDateTime), value); }
            }
        }

        public class Reading
        {
            public static bool NavigateBackAfterReadingAnArticle
            {
                get { return SettingsService.GetValueOrDefault(nameof(NavigateBackAfterReadingAnArticle), true); }
                set { SettingsService.AddOrUpdateValue(nameof(NavigateBackAfterReadingAnArticle), value); }
            }
            public static bool SyncReadingProgress
            {
                get { return SettingsService.GetValueOrDefault(nameof(SyncReadingProgress), default(bool)); }
                set { SettingsService.AddOrUpdateValue(nameof(SyncReadingProgress), value); }
            }
            public static WallabagVideoOpenMode VideoOpenMode
            {
                get { return SettingsService.GetValueOrDefault(nameof(VideoOpenMode), WallabagVideoOpenMode.Inline); }
                set { SettingsService.AddOrUpdateValue(nameof(VideoOpenMode), value); }
            }

            /// <summary>
            /// Sets how videos should be opened.
            /// <para>Short explanation:</para>
            /// <para>- Inline: Replace videos by static thumbnails, clicking them will load the video</para>
            /// <para>- Browser: Open the browser displaying the video</para>
            /// <para>- App: In case a YouTube app is installed, it will be opened, otherwise use inline as fallback</para>
            /// </summary>
            public enum WallabagVideoOpenMode
            {
                Inline,
                Browser,
                App
            }
        }

        public class Appereance
        {
            public static string ColorScheme
            {
                get { return SettingsService.GetValueOrDefault(nameof(ColorScheme), "light"); }
                set { SettingsService.AddOrUpdateValue(nameof(ColorScheme), value); }
            }
            public static int FontSize
            {
                get { return SettingsService.GetValueOrDefault(nameof(FontSize), 16); }
                set { SettingsService.AddOrUpdateValue(nameof(FontSize), value); }
            }
            public static string FontFamily
            {
                get { return SettingsService.GetValueOrDefault(nameof(FontFamily), "serif"); }
                set { SettingsService.AddOrUpdateValue(nameof(FontFamily), value); }
            }
            public static string TextAlignment
            {
                get { return SettingsService.GetValueOrDefault(nameof(TextAlignment), "left"); }
                set { SettingsService.AddOrUpdateValue(nameof(TextAlignment), value); }
            }
        }

        public class BackgroundTask
        {
            public static bool IsEnabled
            {
                get { return SettingsService.GetValueOrDefault(nameof(IsEnabled), default(bool)); }
                set { SettingsService.AddOrUpdateValue(nameof(IsEnabled), value); }
            }
            public static TimeSpan ExecutionInterval
            {
                get { return SettingsService.GetValueOrDefault(nameof(ExecutionInterval), TimeSpan.FromMinutes(15)); }
                set { SettingsService.AddOrUpdateValue(nameof(ExecutionInterval), value); }
            }
            public static bool DownloadNewItemsDuringExecution
            {
                get { return SettingsService.GetValueOrDefault(nameof(DownloadNewItemsDuringExecution), true); }
                set { SettingsService.AddOrUpdateValue(nameof(DownloadNewItemsDuringExecution), value); }
            }
            public static DateTime LastExecution
            {
                get { return SettingsService.GetValueOrDefault(nameof(LastExecution), DateTime.MinValue); }
                set { SettingsService.AddOrUpdateValue(nameof(LastExecution), value); }
            }
        }

        internal static ISettingsService SettingsService => SimpleIoc.Default.GetInstance<ISettingsService>();
    }
}

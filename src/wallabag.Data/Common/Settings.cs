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
                get { return SettingsService.GetValueOrDefault<Uri>(nameof(WallabagUri), containerName: nameof(Authentication)); }
                set { SettingsService.AddOrUpdateValue(nameof(WallabagUri), value, containerName: nameof(Authentication)); }
            }
            public static string ClientId
            {
                get { return SettingsService.GetValueOrDefault<string>(nameof(ClientId), containerName: nameof(Authentication)); }
                set { SettingsService.AddOrUpdateValue(nameof(ClientId), value, containerName: nameof(Authentication)); }
            }
            public static string ClientSecret
            {
                get { return SettingsService.GetValueOrDefault<string>(nameof(ClientSecret), containerName: nameof(Authentication)); }
                set { SettingsService.AddOrUpdateValue(nameof(ClientSecret), value, containerName: nameof(Authentication)); }
            }
            public static string AccessToken
            {
                get { return SettingsService.GetValueOrDefault<string>(nameof(AccessToken), containerName: nameof(Authentication)); }
                set { SettingsService.AddOrUpdateValue(nameof(AccessToken), value, containerName: nameof(Authentication)); }
            }
            public static string RefreshToken
            {
                get { return SettingsService.GetValueOrDefault<string>(nameof(RefreshToken), containerName: nameof(Authentication)); }
                set { SettingsService.AddOrUpdateValue(nameof(RefreshToken), value, containerName: nameof(Authentication)); }
            }
            public static DateTime LastTokenRefreshDateTime
            {
                get { return SettingsService.GetValueOrDefault(nameof(LastTokenRefreshDateTime), DateTime.Now - TimeSpan.FromHours(1), containerName: nameof(Authentication)); }
                set { SettingsService.AddOrUpdateValue(nameof(LastTokenRefreshDateTime), value, containerName: nameof(Authentication)); }
            }
        }

        public class General
        {
            public static string AppVersion
            {
                get { return SettingsService.GetValueOrDefault(nameof(AppVersion), string.Empty, containerName: nameof(General)); }
                set { SettingsService.AddOrUpdateValue(nameof(AppVersion), value, containerName: nameof(General)); }
            }
            public static bool SyncOnStartup
            {
                get { return SettingsService.GetValueOrDefault(nameof(SyncOnStartup), true, containerName: nameof(General)); }
                set { SettingsService.AddOrUpdateValue(nameof(SyncOnStartup), value, containerName: nameof(General)); }
            }
            public static bool AllowCollectionOfTelemetryData
            {
                get { return SettingsService.GetValueOrDefault(nameof(AllowCollectionOfTelemetryData), true, containerName: nameof(General)); }
                set { SettingsService.AddOrUpdateValue(nameof(AllowCollectionOfTelemetryData), value, containerName: nameof(General)); }
            }
            public static DateTime LastSuccessfulSyncDateTime
            {
                get { return SettingsService.GetValueOrDefault<DateTime>(nameof(LastSuccessfulSyncDateTime), containerName: nameof(General)); }
                set { SettingsService.AddOrUpdateValue(nameof(LastSuccessfulSyncDateTime), value, containerName: nameof(General)); }
            }
        }

        public class Reading
        {
            public static bool NavigateBackAfterReadingAnArticle
            {
                get { return SettingsService.GetValueOrDefault(nameof(NavigateBackAfterReadingAnArticle), true, containerName: nameof(Reading)); }
                set { SettingsService.AddOrUpdateValue(nameof(NavigateBackAfterReadingAnArticle), value, containerName: nameof(Reading)); }
            }
            public static bool SyncReadingProgress
            {
                get { return SettingsService.GetValueOrDefault<bool>(nameof(SyncReadingProgress), containerName: nameof(Reading)); }
                set { SettingsService.AddOrUpdateValue(nameof(SyncReadingProgress), value, containerName: nameof(Reading)); }
            }
            public static WallabagVideoOpenMode VideoOpenMode
            {
                get { return SettingsService.GetValueOrDefault(nameof(VideoOpenMode), WallabagVideoOpenMode.Inline, containerName: nameof(Reading)); }
                set { SettingsService.AddOrUpdateValue(nameof(VideoOpenMode), value, containerName: nameof(Reading)); }
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
                get { return SettingsService.GetValueOrDefault(nameof(ColorScheme), "light", containerName: nameof(Appereance)); }
                set { SettingsService.AddOrUpdateValue(nameof(ColorScheme), value, containerName: nameof(Appereance)); }
            }
            public static int FontSize
            {
                get { return SettingsService.GetValueOrDefault(nameof(FontSize), 16, containerName: nameof(Appereance)); }
                set { SettingsService.AddOrUpdateValue(nameof(FontSize), value, containerName: nameof(Appereance)); }
            }
            public static string FontFamily
            {
                get { return SettingsService.GetValueOrDefault(nameof(FontFamily), "serif", containerName: nameof(Appereance)); }
                set { SettingsService.AddOrUpdateValue(nameof(FontFamily), value, containerName: nameof(Appereance)); }
            }
            public static string TextAlignment
            {
                get { return SettingsService.GetValueOrDefault(nameof(TextAlignment), "left", containerName: nameof(Appereance)); }
                set { SettingsService.AddOrUpdateValue(nameof(TextAlignment), value, containerName: nameof(Appereance)); }
            }
        }

        public class BackgroundTask
        {
            public static bool IsEnabled
            {
                get { return SettingsService.GetValueOrDefault<bool>(nameof(IsEnabled), containerName: nameof(BackgroundTask)); }
                set { SettingsService.AddOrUpdateValue(nameof(IsEnabled), value, containerName: nameof(BackgroundTask)); }
            }
            public static TimeSpan ExecutionInterval
            {
                get { return SettingsService.GetValueOrDefault(nameof(ExecutionInterval), TimeSpan.FromMinutes(15), containerName: nameof(BackgroundTask)); }
                set { SettingsService.AddOrUpdateValue(nameof(ExecutionInterval), value, containerName: nameof(BackgroundTask)); }
            }
            public static bool DownloadNewItemsDuringExecution
            {
                get { return SettingsService.GetValueOrDefault(nameof(DownloadNewItemsDuringExecution), true, containerName: nameof(BackgroundTask)); }
                set { SettingsService.AddOrUpdateValue(nameof(DownloadNewItemsDuringExecution), value, containerName: nameof(BackgroundTask)); }
            }
            public static DateTime LastExecution
            {
                get { return SettingsService.GetValueOrDefault<DateTime>(nameof(LastExecution), containerName: nameof(BackgroundTask)); }
                set { SettingsService.AddOrUpdateValue(nameof(LastExecution), value, containerName: nameof(BackgroundTask)); }
            }
        }

        public static void Configure(ISettingsService service) => SimpleIoc.Default.Register<ISettingsService>(() => service);
        public static ISettingsService SettingsService
        {
            get
            {
                if (SimpleIoc.Default.IsRegistered<ISettingsService>())
                    return SimpleIoc.Default.GetInstance<ISettingsService>();
                else
                    throw new NotImplementedException($"The {nameof(SettingsService)} wasn't configured yet. Please configure it using the {nameof(Configure)} method.");
            }
        }
    }
}

using System;
using Template10.Services.SettingsService;

namespace wallabag.Services
{
    public class SettingsService
    {
        #region Singleton & Initialization

        public static readonly SettingsService Instance;
        static SettingsService() { Instance = Instance ?? new SettingsService(); }

        SettingsHelper _helper;
        private SettingsService() { _helper = new SettingsHelper(); }

        #endregion

        #region Authentication

        public Uri WallabagUrl
        {
            get { return _helper.Read(nameof(WallabagUrl), default(Uri)); }
            set { _helper.Write(nameof(WallabagUrl), value); }
        }
        public string ClientId
        {
            get { return _helper.Read(nameof(ClientId), string.Empty); }
            set { _helper.Write(nameof(ClientId), value); }
        }
        public string ClientSecret
        {
            get { return _helper.Read(nameof(ClientSecret), string.Empty); }
            set { _helper.Write(nameof(ClientSecret), value); }
        }
        public string AccessToken
        {
            get { return _helper.Read(nameof(AccessToken), string.Empty); }
            set { _helper.Write(nameof(AccessToken), value); }
        }
        public string RefreshToken
        {
            get { return _helper.Read(nameof(RefreshToken), string.Empty); }
            set { _helper.Write(nameof(RefreshToken), value); }
        }
        public DateTime LastTokenRefreshDateTime
        {
            get { return _helper.Read(nameof(LastTokenRefreshDateTime), DateTime.UtcNow - TimeSpan.FromHours(1)); }
            set { _helper.Write(nameof(LastTokenRefreshDateTime), value); }
        }

        #endregion

        #region General

        public bool SyncOnStartup
        {
            get { return _helper.Read(nameof(SyncOnStartup), true); }
            set { _helper.Write(nameof(SyncOnStartup), value); }
        }

        /// <summary>
        /// Because it's a beta, the default value is set to true. In the final release it will be set to false by default.
        /// </summary>
        public bool AllowCollectionOfTelemetryData
        {
            get { return _helper.Read(nameof(AllowCollectionOfTelemetryData), true); }
            set { _helper.Write(nameof(AllowCollectionOfTelemetryData), value); }
        }

        #endregion

        #region Reading

        public bool NavigateBackAfterReadingAnArticle
        {
            get { return _helper.Read(nameof(NavigateBackAfterReadingAnArticle), true); }
            set { _helper.Write(nameof(NavigateBackAfterReadingAnArticle), value); }
        }
        public bool SyncReadingProgress
        {
            get { return _helper.Read(nameof(SyncReadingProgress), false); }
            set { _helper.Write(nameof(SyncReadingProgress), value); }
        }

        #endregion

        #region Appereance

        public string ColorScheme
        {
            get { return _helper.Read(nameof(ColorScheme), "light"); }
            set { _helper.Write(nameof(ColorScheme), value); }
        }
        public int FontSize
        {
            get { return _helper.Read(nameof(FontSize), 16); }
            set { _helper.Write(nameof(FontSize), value); }
        }
        public string FontFamily
        {
            get { return _helper.Read(nameof(FontFamily), "sans"); }
            set { _helper.Write(nameof(FontFamily), value); }
        }
        public string TextAlignment
        {
            get { return _helper.Read(nameof(TextAlignment), "left"); }
            set { _helper.Write(nameof(TextAlignment), value); }
        }

        #endregion
    }
}

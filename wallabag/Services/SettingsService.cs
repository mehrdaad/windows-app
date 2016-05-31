using System;
using Template10.Services.SettingsService;
using Windows.UI;

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

        #region Appereance

        public string ColorScheme
        {
            get { return _helper.Read(nameof(ColorScheme), "sepia"); }
            set { _helper.Write(nameof(ColorScheme), value); }
        }

        #endregion
    }
}

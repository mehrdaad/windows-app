using System;
using Template10.Services.SettingsService;

namespace wallabag.Services
{
    public class SettingsService
    {
        public static readonly SettingsService Instance;
        static SettingsService() { Instance = Instance ?? new SettingsService(); }

        SettingsHelper _helper;
        private SettingsService() { _helper = new SettingsHelper(); }

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
    }
}

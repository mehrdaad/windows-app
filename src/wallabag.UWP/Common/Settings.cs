using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallabag.Common
{
    class Settings : Data.Common.Settings
    {
        public class CustomSettings
        {
            public static bool WhiteOverlayForTitleBar
            {
                get { return SettingsService.GetValueOrDefault<bool>(nameof(WhiteOverlayForTitleBar), containerName: nameof(Settings.Appereance)); }
                set { SettingsService.AddOrUpdateValue(nameof(WhiteOverlayForTitleBar), value, containerName: nameof(Settings.Appereance)); }
            }
        }
    }
}

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
                get { return SettingsService.GetValueOrDefault(nameof(WhiteOverlayForTitleBar), default(bool)); }
                set { SettingsService.AddOrUpdateValue(nameof(WhiteOverlayForTitleBar), value); }
            }
        }
    }
}

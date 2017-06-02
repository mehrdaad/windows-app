using GalaSoft.MvvmLight.Ioc;
using wallabag.Services;

namespace wallabag.Common
{
    public class Settings : Data.Common.Settings
    {
        public class LiveTile
        {
            private static LiveTileService _liveTileService => SimpleIoc.Default.GetInstance<LiveTileService>();
            public static bool IsEnabled
            {
                get => SettingsService.GetValueOrDefault(nameof(IsEnabled), true, containerName: nameof(LiveTile));
                set
                {
                    SettingsService.AddOrUpdateValue(nameof(IsEnabled), value, containerName: nameof(LiveTile));
                    _liveTileService.UpdateTile();
                }
            }

            public static bool BadgeIsEnabled
            {
                get => SettingsService.GetValueOrDefault(nameof(BadgeIsEnabled), false, containerName: nameof(LiveTile));
                set
                {
                    SettingsService.AddOrUpdateValue(nameof(BadgeIsEnabled), value, containerName: nameof(LiveTile));
                    _liveTileService.UpdateBadge();
                }
            }

            public static ItemType DisplayedItemType
            {
                get => SettingsService.GetValueOrDefault(nameof(DisplayedItemType), ItemType.Unread, containerName: nameof(LiveTile));
                set
                {
                    SettingsService.AddOrUpdateValue(nameof(DisplayedItemType), value, containerName: nameof(LiveTile));
                    _liveTileService.UpdateAll();
                }
            }

            public enum ItemType { Unread, Starred, Archived }
        }
    }
}

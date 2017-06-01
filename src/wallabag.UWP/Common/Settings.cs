using GalaSoft.MvvmLight.Ioc;
using wallabag.Services;

namespace wallabag.Common
{
    public class Settings : Data.Common.Settings
    {
        public class LiveTile
        {
            private static void UpdateLiveTile() => SimpleIoc.Default.GetInstance<LiveTileService>().Update();

            public static bool IsEnabled
            {
                get => SettingsService.GetValueOrDefault(nameof(IsEnabled), true, containerName: nameof(LiveTile));
                set
                {
                    SettingsService.AddOrUpdateValue(nameof(IsEnabled), value, containerName: nameof(LiveTile));
                    UpdateLiveTile();
                }
            }

            public static ItemType DisplayedItemType
            {
                get => SettingsService.GetValueOrDefault(nameof(DisplayedItemType), ItemType.Unread, containerName: nameof(LiveTile));
                set
                {
                    SettingsService.AddOrUpdateValue(nameof(DisplayedItemType), value, containerName: nameof(LiveTile));
                    UpdateLiveTile();
                }
            }

            public enum ItemType { Unread, Starred, Archived }
        }
    }
}

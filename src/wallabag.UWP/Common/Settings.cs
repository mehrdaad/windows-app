namespace wallabag.Common
{
    public class Settings : Data.Common.Settings
    {
        public class LiveTile
        {
            public static bool IsEnabled
            {
                get => SettingsService.GetValueOrDefault(nameof(IsEnabled), true, containerName: nameof(LiveTile));
                set => SettingsService.AddOrUpdateValue(nameof(IsEnabled), value, containerName: nameof(LiveTile));
            }

            public static ItemType DisplayedItemType
            {
                get => SettingsService.GetValueOrDefault(nameof(DisplayedItemType), ItemType.Unread, containerName: nameof(LiveTile));
                set => SettingsService.AddOrUpdateValue(nameof(DisplayedItemType), value, containerName: nameof(LiveTile));
            }

            public enum ItemType { Unread, Starred, Archived }
        }
    }
}

using Microsoft.Toolkit.Uwp.Notifications;
using SQLite.Net;
using System;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using Windows.UI.Notifications;

namespace wallabag.Services
{
    public class LiveTileService
    {
        private SQLiteConnection _database;
        private ILoggingService _logging;
        private IPlatformSpecific _device;

        public LiveTileService(
            SQLiteConnection database,
            ILoggingService logging,
            IPlatformSpecific platform)
        {
            _database = database;
            _logging = logging;
            _device = platform;
        }

        public void Update()
        {
            _logging.WriteLine("Updating live tile.");

            _logging.WriteLine("Fetching unread items from the database.");
            var items = _database.Query<Item>("select Title,PreviewImageUri,EstimatedReadingTime from Item where IsRead=0 ORDER BY CreationDate DESC");
            int maxPossibleTiles = Math.Min(5, items.Count);

            for (int i = 0; i < maxPossibleTiles; i++)
            {
                var item = items[i];

                _logging.WriteLine($"Creating live tile for '{item.Title}'.");

                string imageSource = item.PreviewImageUri?.OriginalString ?? string.Empty;
                var tileContent = new TileContent()
                {
                    Visual = new TileVisual()
                    {
                        TileWide = CreateTileBinding(TileSize.Wide, item.Title, item.EstimatedReadingTime, imageSource),
                        TileMedium = CreateTileBinding(TileSize.Medium, item.Title, item.EstimatedReadingTime, imageSource),
                        TileLarge = CreateTileBinding(TileSize.Large, item.Title, item.EstimatedReadingTime, imageSource)
                    }
                };

                // Create the tile notification
                var tileNotification = new TileNotification(tileContent.GetXml()) { Tag = $"item-{i}" };

                // And send the notification to the primary tile
                var tu = TileUpdateManager.CreateTileUpdaterForApplication();
                tu.EnableNotificationQueue(true);
                tu.Update(tileNotification);
            }
        }

        private TileBinding CreateTileBinding(TileSize size, string title, int readingTime, string imageSource)
        {
            var binding = new TileBinding()
            {
                Branding = TileBranding.Logo,
                Content = new TileBindingContentAdaptive()
            };

            // Create the XML in code
            var group = new AdaptiveGroup();
            var subgroup = new AdaptiveSubgroup();

            // Add the title to the tile
            var tileTitle = new AdaptiveText()
            {
                Text = title,
                HintMaxLines = size == TileSize.Large ? 3 : 2,
                HintWrap = true,
                HintStyle = size == TileSize.Medium ? AdaptiveTextStyle.Caption : AdaptiveTextStyle.Body
            };

            subgroup.Children.Add(tileTitle);

            // Add the reading time
            subgroup.Children.Add(new AdaptiveText()
            {
                Text = $"Lesezeit: drei Minuten",
                HintStyle = AdaptiveTextStyle.CaptionSubtle
            });

            // Finally add the subgroup to the tile
            group.Children.Add(subgroup);

            // Add the background image if neccessary
            if (!string.IsNullOrEmpty(imageSource))
            {
                (binding.Content as TileBindingContentAdaptive).BackgroundImage = new TileBackgroundImage()
                {
                    Source = imageSource,
                    HintOverlay = 40
                };
            }

            // Add the group to the tile
            (binding.Content as TileBindingContentAdaptive).Children.Add(group);

            return binding;
        }

        private enum TileSize { Small, Medium, Wide, Large }
    }
}

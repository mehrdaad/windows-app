using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using wallabag.Data.Common;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Services;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;

namespace wallabag.Data.ViewModels
{
    public class ItemPageViewModel : ViewModelBase
    {
        private IOfflineTaskService _offlineTaskService => SimpleIoc.Default.GetInstance<IOfflineTaskService>();

        public ItemViewModel Item { get; set; }
        public string FormattedHtml { get; set; }

        public bool FailureHasHappened { get; set; }
        public string FailureEmoji { get; set; }
        public string FailureDescription { get; set; }

        private FontFamily _iconFontFamily = new FontFamily("Segoe MDL2 Assets");
        private const string m_READGLYPH = "\uE001";
        private const string m_UNREADGLYPH = "\uE18B";
        private const string m_STARREDGLYPH = "\uE006";
        private const string m_UNSTARREDGLYPH = "\uE007";
        public FontIcon ChangeReadStatusButtonFontIcon { get; set; }
        public FontIcon ChangeFavoriteStatusButtonFontIcon { get; set; }
        public ICommand ChangeReadStatusCommand { get; private set; }
        public ICommand ChangeFavoriteStatusCommand { get; private set; }
        public ICommand EditTagsCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public SolidColorBrush ForegroundBrush { get; set; }
        public SolidColorBrush BackgroundBrush { get; set; }
        public ElementTheme ColorApplicationTheme { get; set; }

        public int FontSize { get; set; } = Settings.Appereance.FontSize;
        public string FontFamily { get; set; } = Settings.Appereance.FontFamily;
        public string ColorScheme { get; set; } = Settings.Appereance.ColorScheme;
        public string TextAlignment { get; set; } = Settings.Appereance.TextAlignment;

        public Uri RightClickUri { get; set; }
        public ICommand SaveRightClickLinkCommand { get; private set; }
        public ICommand OpenRightClickLinkInBrowserCommand { get; private set; }

        public ItemPageViewModel()
        {
            _loggingService.WriteLine($"Initializing new instance of {nameof(ItemPageViewModel)}.");

            ChangeReadStatusCommand = new RelayCommand(() => ChangeReadStatus());
            ChangeFavoriteStatusCommand = new RelayCommand(() => ChangeFavoriteStatus());
            EditTagsCommand = new RelayCommand(() => _navigationService.Navigate(Navigation.Pages.EditTagsPage, new EditTagsViewModel(Item.Model)));
            DeleteCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine("Deleting the current item.");
                Item.DeleteCommand.Execute();
                _navigationService.GoBack();
            });

            SaveRightClickLinkCommand = new RelayCommand(() => _offlineTaskService.Add(RightClickUri.ToString(), new List<string>()));
            OpenRightClickLinkInBrowserCommand = new RelayCommand(async () => await Launcher.LaunchUriAsync(RightClickUri));
        }

        private async Task GenerateFormattedHtmlAsync()
        {
            _loggingService.WriteLine("Generating the HTML.");
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Article/article.html"));
            string _template = await FileIO.ReadTextAsync(file);

            _loggingService.WriteLineIf(string.IsNullOrEmpty(_template), "The template is empty!", LoggingCategory.Critical);

            string accentColor = Application.Current.Resources["SystemAccentColor"].ToString().Remove(1, 2);
            var styleSheetBuilder = new StringBuilder();
            styleSheetBuilder.Append("<style>");
            styleSheetBuilder.Append("hr {border-color: " + accentColor + " !important}");
            styleSheetBuilder.Append("::selection,mark {background: " + accentColor + " !important}");
            styleSheetBuilder.Append("body {");
            styleSheetBuilder.Append($"font-size: {FontSize}px;");
            styleSheetBuilder.Append($"text-align: {TextAlignment};}}");
            styleSheetBuilder.Append("</style>");

            string imageHeader = string.Empty;

            if (Item.Model.Hostname.Contains("youtube.com") == false &&
                Item.Model.Hostname.Contains("vimeo.com") == false &&
                Item.Model.PreviewImageUri != null)
            {
                _loggingService.WriteLine($"Image header is set to: {Item.Model.PreviewImageUri.ToString()}");
                imageHeader = Item.Model.PreviewImageUri.ToString();
            }
            else
            {
                _loggingService.WriteLine("Image header is empty.");
            }

            _loggingService.WriteLine("Formatting the template with the item properties.");
            FormattedHtml = _template.FormatWith(new
            {
                title = Item.Model.Title,
                content = await SetupArticleForHtmlViewerAsync(),
                articleUrl = Item.Model.Url,
                hostname = Item.Model.Hostname,
                color = ColorScheme,
                font = FontFamily,
                progress = Item.Model.ReadingProgress,
                publishDate = string.Format("{0:d}", Item.Model.CreationDate),
                stylesheet = styleSheetBuilder.ToString(),
                imageHeader = imageHeader
            });
        }

        private async Task<string> SetupArticleForHtmlViewerAsync()
        {
            _loggingService.WriteLine("Preparing HTML.");
            var document = new HtmlDocument();
            document.LoadHtml(Item.Model.Content);
            document.OptionCheckSyntax = false;

            // Implement lazy-loading for images
            _loggingService.WriteLine("Implementing lazy-loading for images...");
            foreach (var node in document.DocumentNode.Descendants("img"))
            {
                if (node.HasAttributes && node.Attributes["src"] != null)
                {
                    string oldSource = node.Attributes["src"].Value;
                    node.Attributes.RemoveAll();

                    _loggingService.WriteLine($"Source of the image: {oldSource}");

                    if (!oldSource.Equals(Item.Model.PreviewImageUri?.ToString()) &&
                        GeneralHelper.InternetConnectionIsAvailable)
                    {
                        node.Attributes.Add("src", "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
                        node.Attributes.Add("data-src", oldSource);
                        node.Attributes.Add("class", "lazy");
                    }

                    node.InnerHtml = " "; // dirty hack to let HtmlAgilityPack close the <img> tag
                }
            }

            _loggingService.WriteLine("Add thumbnails for embedded videos...");
            string dataOpenMode = Settings.Reading.VideoOpenMode.ToString().ToLower();
            string containerString = "<div class='wallabag-video' style='background-image: url({0})' data-open-mode='" + dataOpenMode + "' data-provider='{1}' data-video-id='{2}'><span></span></div>";

            // Replace videos (YouTube & Vimeo) by static thumbnails                  
            var iframeNodes = document.DocumentNode.Descendants("iframe").ToList();
            var videoNodes = document.DocumentNode.Descendants("video").ToList();

            _loggingService.WriteLine($"Number of <iframe>'s: {iframeNodes.Count}");
            _loggingService.WriteLine($"Number of <video>'s: {videoNodes.Count}");

            _loggingService.WriteLine("Handling iframes...");
            foreach (var node in iframeNodes)
            {
                if (node.HasAttributes &&
                    node.Attributes.Contains("src"))
                {
                    var videoSourceUri = new Uri(node.Attributes["src"].Value);
                    string videoId = videoSourceUri.Segments.Last();
                    string videoProvider = videoSourceUri.Host;

                    if (videoProvider.Contains("youtube.com"))
                        videoProvider = "youtube";
                    else if (videoProvider.Contains("player.vimeo.com"))
                        videoProvider = "vimeo";

                    _loggingService.WriteLine($"Video source: {videoSourceUri}");
                    _loggingService.WriteLine($"Video ID: {videoId}");
                    _loggingService.WriteLine($"Video provider: {videoProvider}");
                    string newContainer = string.Format(containerString, await GetPreviewImageForVideoAsync(videoProvider, videoId), videoProvider, videoId);

                    node.ParentNode.InsertAfter(HtmlNode.CreateNode(newContainer), node);
                    node.ParentNode.RemoveChild(node);
                }
            }

            // This loop is for HTML5 videos using the <video> tag
            _loggingService.WriteLine("Handling video tags...");
            foreach (var node in videoNodes)
            {
                string videoSource = string.Empty;

                videoSource = node.GetAttributeValue("src", string.Empty);

                if (string.IsNullOrEmpty(videoSource) && node.HasChildNodes)
                    videoSource = node.ChildNodes
                          .Where(i => i.Name.Equals("source") && i.GetAttributeValue("type", string.Empty).Equals("video/mp4"))
                          .FirstOrDefault()
                          ?.GetAttributeValue("src", string.Empty);

                _loggingService.WriteLine($"Video source: {videoSource}");
                if (!string.IsNullOrEmpty(videoSource))
                {
                    string newContainer = string.Format(containerString, string.Empty, "html", videoSource);

                    node.ParentNode.InsertAfter(HtmlNode.CreateNode(newContainer), node);
                    node.ParentNode.RemoveChild(node);
                }
            }

            return document.DocumentNode.OuterHtml;
        }

        private async Task<string> GetPreviewImageForVideoAsync(string videoProvider, string videoId)
        {
            string result = string.Empty;

            _loggingService.WriteLine($"Getting preview image for video {videoId} from {videoProvider}");

            if (videoProvider == "youtube")
                result = $"http://img.youtube.com/vi/{videoId}/0.jpg";
            else
            {
                string link = $"http://vimeo.com/api/v2/video/{videoId}.json";
                _loggingService.WriteLine($"Contacting vimeo for preview image: {link}");

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(new Uri(link));
                    _loggingService.WriteObject(response);

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                        _loggingService.WriteLine($"Resulted JSON: {json}");
                        result = json[0].thumbnail_large.Value;
                    }
                }
            }

            _loggingService.WriteLine($"Final result: {result}");
            return result;
        }

        private void ChangeReadStatus()
        {
            _loggingService.WriteLine("Changing read status of item.");
            if (Item.Model.IsRead)
                Item.UnmarkAsReadCommand.Execute();
            else
            {
                Item.MarkAsReadCommand.Execute();

                if (Settings.Reading.NavigateBackAfterReadingAnArticle)
                {
                    _loggingService.WriteLine("Navigating back to main page.");
                    _navigationService.GoBack();
                }

                if (Settings.Reading.SyncReadingProgress)
                {
                    _loggingService.WriteLine($"Deleting reading progress.");
                    var readingSettingsContainer = ApplicationData.Current.RoamingSettings.CreateContainer($"ReadingProgressContainer-{Settings.Authentication.ClientId}", ApplicationDataCreateDisposition.Always);
                    if (readingSettingsContainer.Values.ContainsKey(Item.Model.Id.ToString()))
                        _loggingService.WriteLine($"Success: {readingSettingsContainer.Values.Remove(Item.Model.Id.ToString())}");
                }
            }

            UpdateReadIcon();
        }
        private void ChangeFavoriteStatus()
        {
            _loggingService.WriteLine("Changing favorite status of item.");
            if (Item.Model.IsStarred)
                Item.UnmarkAsStarredCommand.Execute();
            else
                Item.MarkAsStarredCommand.Execute();

            UpdateFavoriteIcon();
        }
        private void UpdateReadIcon()
        {
            _loggingService.WriteLine("Updating read icon.");

            if (Item.Model.IsRead)
                ChangeReadStatusButtonFontIcon = CreateFontIcon(m_UNREADGLYPH);
            else
                ChangeReadStatusButtonFontIcon = CreateFontIcon(m_READGLYPH);

            _loggingService.WriteLine($"New glyph: {ChangeReadStatusButtonFontIcon.Glyph}");
        }
        private void UpdateFavoriteIcon()
        {
            _loggingService.WriteLine("Updating favorite icon.");

            if (Item.Model.IsStarred)
                ChangeFavoriteStatusButtonFontIcon = CreateFontIcon(m_UNSTARREDGLYPH);
            else
                ChangeFavoriteStatusButtonFontIcon = CreateFontIcon(m_STARREDGLYPH);

            _loggingService.WriteLine($"New glyph: {ChangeFavoriteStatusButtonFontIcon.Glyph}");
        }
        public void UpdateBrushes()
        {
            _loggingService.WriteLine($"Updating brushes for theme '{ColorScheme}'");
            if (ColorScheme.Equals("light"))
            {
                ForegroundBrush = Color.FromArgb(0xFF, 0x44, 0x44, 0x44).ToSolidColorBrush();
                BackgroundBrush = Colors.White.ToSolidColorBrush();
                ColorApplicationTheme = ElementTheme.Light;
            }
            else if (ColorScheme.Equals("sepia"))
            {
                ForegroundBrush = Colors.Maroon.ToSolidColorBrush();
                BackgroundBrush = Colors.Beige.ToSolidColorBrush();
                ColorApplicationTheme = ElementTheme.Light;
            }
            else if (ColorScheme.Equals("dark"))
            {
                ForegroundBrush = Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC).ToSolidColorBrush();
                BackgroundBrush = Color.FromArgb(0xFF, 0x33, 0x33, 0x33).ToSolidColorBrush();
                ColorApplicationTheme = ElementTheme.Dark;
            }
            else if (ColorScheme.Equals("black"))
            {
                ForegroundBrush = Color.FromArgb(0xFF, 0xB3, 0xB3, 0xB3).ToSolidColorBrush();
                BackgroundBrush = Colors.Black.ToSolidColorBrush();
                ColorApplicationTheme = ElementTheme.Dark;
            }
        }

        private FontIcon CreateFontIcon(string glyph) => new FontIcon() { Glyph = glyph, FontFamily = _iconFontFamily };

        public override async Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state)
        {
            _loggingService.WriteLine($"Navigation parameter: {parameter}");
            Item = ItemViewModel.FromId((int)parameter);

            _loggingService.WriteLineIf(Item == null, "Item is null.", LoggingCategory.Warning);
            _loggingService.WriteLine($"Item title: {Item?.Model?.Title}");

            if ((Item == null || string.IsNullOrEmpty(Item?.Model?.Content)) && GeneralHelper.InternetConnectionIsAvailable)
            {
                _loggingService.WriteLine("Fetching item from server.");
                var item = await _client.GetItemAsync(Item.Model.Id);
                if (item != null)
                    Item = new ItemViewModel(item);

                _loggingService.WriteLine($"Success: {item != null}");
                _loggingService.WriteObject(item);
            }

            if (string.IsNullOrEmpty(Item?.Model?.Content))
            {
                _loggingService.WriteLine("No content available.", LoggingCategory.Warning);
                FailureHasHappened = true;
                FailureEmoji = "😶";
                FailureDescription = GeneralHelper.LocalizedResource("NoContentAvailableErrorMessage");
            }
            else if (Item?.Model?.Content?.Contains("wallabag can't retrieve contents for this article.") == true)
            {
                _loggingService.WriteLine("wallabag can't retrieve content.", LoggingCategory.Warning);
                FailureHasHappened = true;
                FailureEmoji = "😈";
                FailureDescription = GeneralHelper.LocalizedResource("CantRetrieveContentsErrorMessage");
            }

            UpdateReadIcon();
            UpdateFavoriteIcon();
            UpdateBrushes();

            if (Settings.Reading.SyncReadingProgress && Item.Model.ReadingProgress < 100)
            {
                _loggingService.WriteLine("Fetching reading progress from roaming settings.");
                var readingSettingsContainer = ApplicationData.Current.RoamingSettings.CreateContainer($"ReadingProgressContainer-{Settings.Authentication.ClientId}", ApplicationDataCreateDisposition.Always);
                if (readingSettingsContainer.Values.ContainsKey(Item.Model.Id.ToString()))
                    Item.Model.ReadingProgress = (double)readingSettingsContainer.Values[Item.Model.Id.ToString()];

                _loggingService.WriteLine($"Reading progress: {Item.Model.ReadingProgress}");
            }

            await GenerateFormattedHtmlAsync();
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState)
        {
            Settings.Appereance.FontSize = FontSize;
            Settings.Appereance.FontFamily = FontFamily;
            Settings.Appereance.ColorScheme = ColorScheme;
            Settings.Appereance.TextAlignment = TextAlignment;

            _loggingService.WriteLine("Updating item in database.");
            _database.Update(Item.Model);

            if (Settings.Reading.SyncReadingProgress && Item.Model.ReadingProgress < 100)
            {
                _loggingService.WriteLine("Setting reading progress in RoamingSettings.");
                var readingSettingsContainer = ApplicationData.Current.RoamingSettings.CreateContainer($"ReadingProgressContainer-{Settings.Authentication.ClientId}", ApplicationDataCreateDisposition.Always);
                readingSettingsContainer.Values[Item.Model.Id.ToString()] = Item.Model.ReadingProgress;
            }

            return Task.CompletedTask;
        }
    }
}

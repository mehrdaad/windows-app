using HtmlAgilityPack;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Template10.Utils;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Services;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace wallabag.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class ItemPageViewModel : ViewModelBase
    {
        public ItemViewModel Item { get; set; }
        public string FormattedHtml { get; set; }

        public bool FailureHasHappened { get; set; }
        public string FailureEmoji { get; set; }
        public string FailureDescription { get; set; }

        private FontFamily _iconFontFamily = new FontFamily("Segoe MDL2 Assets");
        private const string _readGlyph = "\uE001";
        private const string _unreadGlyph = "\uE18B";
        private const string _starredGlyph = "\uE006";
        private const string _unstarredGlyph = "\uE007";
        public FontIcon ChangeReadStatusButtonFontIcon { get; set; }
        public FontIcon ChangeFavoriteStatusButtonFontIcon { get; set; }
        public DelegateCommand ChangeReadStatusCommand { get; private set; }
        public DelegateCommand ChangeFavoriteStatusCommand { get; private set; }
        public DelegateCommand EditTagsCommand { get; private set; }
        public DelegateCommand DeleteCommand { get; private set; }

        public SolidColorBrush ForegroundBrush { get; set; }
        public SolidColorBrush BackgroundBrush { get; set; }
        public ElementTheme ColorApplicationTheme { get; set; }

        public int FontSize { get; set; } = SettingsService.Instance.FontSize;
        public string FontFamily { get; set; } = SettingsService.Instance.FontFamily;
        public string ColorScheme { get; set; } = SettingsService.Instance.ColorScheme;
        public string TextAlignment { get; set; } = SettingsService.Instance.TextAlignment;

        public Uri RightClickUri { get; set; }
        public DelegateCommand SaveRightClickLinkCommand { get; private set; }
        public DelegateCommand OpenRightClickLinkInBrowserCommand { get; private set; }

        public ItemPageViewModel()
        {
            ChangeReadStatusCommand = new DelegateCommand(() => ChangeReadStatus());
            ChangeFavoriteStatusCommand = new DelegateCommand(() => ChangeFavoriteStatus());
            EditTagsCommand = new DelegateCommand(async () => await Services.DialogService.ShowAsync(Services.DialogService.Dialog.EditTags,
                new EditTagsViewModel(Item.Model),
                ColorApplicationTheme));
            DeleteCommand = new DelegateCommand(() =>
            {
                Item.DeleteCommand.Execute();
                NavigationService.GoBack();
            });

            SaveRightClickLinkCommand = new DelegateCommand(() => OfflineTaskService.Add(RightClickUri.ToString(), new List<string>()));
            OpenRightClickLinkInBrowserCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(RightClickUri));
        }

        private async Task GenerateFormattedHtmlAsync()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Article/article.html"));
            string _template = await FileIO.ReadTextAsync(file);

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
                imageHeader = Item.Model.PreviewImageUri.ToString();

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
            var document = new HtmlDocument();
            document.LoadHtml(Item.Model.Content);
            document.OptionCheckSyntax = false;

            // Implement lazy-loading for images
            foreach (var node in document.DocumentNode.Descendants("img"))
            {
                if (node.HasAttributes && node.Attributes["src"] != null)
                {
                    string oldSource = node.Attributes["src"].Value;
                    node.Attributes.RemoveAll();

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

            string dataOpenMode = SettingsService.Instance.VideoOpenMode.ToString().ToLower();
            string containerString = "<div class='wallabag-video' style='background-image: url({0})' data-open-mode='" + dataOpenMode + "' data-provider='{1}' data-video-id='{2}'><span></span></div>";

            // Replace videos (YouTube & Vimeo) by static thumbnails                  
            var iframeNodes = document.DocumentNode.Descendants("iframe").ToList();
            var videoNodes = document.DocumentNode.Descendants("video").ToList();

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

                    string newContainer = string.Format(containerString, await GetPreviewImageForVideoAsync(videoProvider, videoId), videoProvider, videoId);

                    node.ParentNode.InsertAfter(HtmlNode.CreateNode(newContainer), node);
                    node.ParentNode.RemoveChild(node);
                }
            }

            // This loop is for HTML5 videos using the <video> tag
            foreach (var node in videoNodes)
            {
                string videoSource = string.Empty;

                videoSource = node.GetAttributeValue("src", string.Empty);

                if (string.IsNullOrEmpty(videoSource) && node.HasChildNodes)
                    videoSource = node.ChildNodes
                          .Where(i => i.Name.Equals("source") && i.GetAttributeValue("type", string.Empty).Equals("video/mp4"))
                          .FirstOrDefault()
                          ?.GetAttributeValue("src", string.Empty);

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
            if (videoProvider == "youtube")
                return $"http://img.youtube.com/vi/{videoId}/0.jpg";
            else
            {
                string link = $"http://vimeo.com/api/v2/video/{videoId}.json";
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(new Uri(link));

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                        return json[0].thumbnail_large.Value;
                    }
                    return string.Empty;
                }
            }
        }

        private void ChangeReadStatus()
        {
            if (Item.Model.IsRead)
                Item.UnmarkAsReadCommand.Execute();
            else
            {
                Item.MarkAsReadCommand.Execute();

                if (SettingsService.Instance.NavigateBackAfterReadingAnArticle)
                    NavigationService.GoBack();

                if (SettingsService.Instance.SyncReadingProgress)
                {
                    var readingSettingsContainer = ApplicationData.Current.RoamingSettings.CreateContainer($"ReadingProgressContainer-{SettingsService.Instance.ClientId}", ApplicationDataCreateDisposition.Always);
                    if (readingSettingsContainer.Values.ContainsKey(Item.Model.Id.ToString()))
                        readingSettingsContainer.Values.Remove(Item.Model.Id.ToString());
                }
            }

            UpdateReadIcon();
        }
        private void ChangeFavoriteStatus()
        {
            if (Item.Model.IsStarred)
                Item.UnmarkAsStarredCommand.Execute();
            else
                Item.MarkAsStarredCommand.Execute();

            UpdateFavoriteIcon();
        }
        private void UpdateReadIcon()
        {
            if (Item.Model.IsRead)
                ChangeReadStatusButtonFontIcon = CreateFontIcon(_unreadGlyph);
            else
                ChangeReadStatusButtonFontIcon = CreateFontIcon(_readGlyph);
        }
        private void UpdateFavoriteIcon()
        {
            if (Item.Model.IsStarred)
                ChangeFavoriteStatusButtonFontIcon = CreateFontIcon(_unstarredGlyph);
            else
                ChangeFavoriteStatusButtonFontIcon = CreateFontIcon(_starredGlyph);
        }
        public void UpdateBrushes()
        {
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

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            Item = ItemViewModel.FromId((int)parameter);

            if ((Item == null || string.IsNullOrEmpty(Item?.Model?.Content)) && GeneralHelper.InternetConnectionIsAvailable)
            {
                var item = await App.Client.GetItemAsync(Item.Model.Id);
                if (item != null)
                    Item = new ItemViewModel(item);
            }

            if (string.IsNullOrEmpty(Item?.Model?.Content))
            {
                FailureHasHappened = true;
                FailureEmoji = "😶";
                FailureDescription = GeneralHelper.LocalizedResource("NoContentAvailableErrorMessage");
            }
            else if (Item?.Model?.Content?.Contains("wallabag can't retrieve contents for this article.") == true)
            {
                FailureHasHappened = true;
                FailureEmoji = "😈";
                FailureDescription = GeneralHelper.LocalizedResource("CantRetrieveContentsErrorMessage");
            }

            UpdateReadIcon();
            UpdateFavoriteIcon();
            UpdateBrushes();

            if (SettingsService.Instance.SyncReadingProgress && Item.Model.ReadingProgress < 100)
            {
                var readingSettingsContainer = ApplicationData.Current.RoamingSettings.CreateContainer($"ReadingProgressContainer-{SettingsService.Instance.ClientId}", ApplicationDataCreateDisposition.Always);
                if (readingSettingsContainer.Values.ContainsKey(Item.Model.Id.ToString()))
                    Item.Model.ReadingProgress = (double)readingSettingsContainer.Values[Item.Model.Id.ToString()];
            }

            await GenerateFormattedHtmlAsync();
        }
        public override async Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            SettingsService.Instance.FontSize = FontSize;
            SettingsService.Instance.FontFamily = FontFamily;
            SettingsService.Instance.ColorScheme = ColorScheme;
            SettingsService.Instance.TextAlignment = TextAlignment;

            App.Database.Update(Item.Model);

            if (SettingsService.Instance.SyncReadingProgress && Item.Model.ReadingProgress < 100)
            {
                var readingSettingsContainer = ApplicationData.Current.RoamingSettings.CreateContainer($"ReadingProgressContainer-{SettingsService.Instance.ClientId}", ApplicationDataCreateDisposition.Always);
                readingSettingsContainer.Values[Item.Model.Id.ToString()] = Item.Model.ReadingProgress;
            }

            await TitleBarHelper.ResetAsync();
        }
    }
}

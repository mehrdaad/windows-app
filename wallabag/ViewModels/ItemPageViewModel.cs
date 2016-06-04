using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Template10.Utils;
using wallabag.Common;
using wallabag.Models;
using wallabag.Services;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class ItemPageViewModel : ViewModelBase
    {
        public ItemViewModel Item { get; set; }
        public string FormattedHtml { get; set; }

        private FontFamily _iconFontFamily = new FontFamily("Segoe MDL2 Assets");
        private const string _readGlyph = "\uE001";
        private const string _unreadGlyph = "\uE18B";
        private const string _starredGlyph = "\uE006";
        private const string _unstarredGlyph = "\uE007";
        public FontIcon ChangeReadStatusButtonFontIcon { get; set; }
        public FontIcon ChangeFavoriteStatusButtonFontIcon { get; set; }
        public DelegateCommand ChangeReadStatusCommand { get; private set; }
        public DelegateCommand ChangeFavoriteStatusCommand { get; private set; }

        public SolidColorBrush ForegroundBrush { get; set; }
        public SolidColorBrush BackgroundBrush { get; set; }
        public ElementTheme ColorApplicationTheme { get; set; }

        public int FontSize { get; set; } = 16;
        public string FontFamily { get; set; } = "sans";
        public string ColorScheme { get; set; } = "light";
        public string TextAlignment { get; set; } = "left";
        
        private async Task GenerateFormattedHtmlAsync()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Article/article.html"));
            string _template = await FileIO.ReadTextAsync(file);

            string accentColor = Application.Current.Resources["SystemAccentColor"].ToString().Remove(1, 2);
            StringBuilder styleSheetBuilder = new StringBuilder();
            styleSheetBuilder.Append("<style>");
            styleSheetBuilder.Append("hr {border-color: " + accentColor + " !important}");
            styleSheetBuilder.Append("::selection,mark {background: " + accentColor + " !important}");
            styleSheetBuilder.Append("body {");
            styleSheetBuilder.Append($"font-size: {FontSize}px;");
            styleSheetBuilder.Append($"text-align: {TextAlignment};");
            styleSheetBuilder.Append("}</style>");

            FormattedHtml = _template.FormatWith(new
            {
                title = Item.Model.Title,
                content = Item.Model.Content,
                articleUrl = Item.Model.Url,
                hostname = Item.Model.Hostname,
                color = ColorScheme,
                font = FontFamily,
                progress = "0",
                publishDate = string.Format("{0:d}", Item.Model.CreationDate),
                stylesheet = styleSheetBuilder.ToString()
            });
        }

        private void ChangeReadStatus()
        {
            if (Item.Model.IsRead)
                Item.UnmarkAsReadCommand.Execute();
            else
                Item.MarkAsReadCommand.Execute();

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
                ChangeReadStatusButtonFontIcon = CreateFontIcon(_readGlyph);
            else
                ChangeReadStatusButtonFontIcon = CreateFontIcon(_unreadGlyph);
        }
        private void UpdateFavoriteIcon()
        {
            if (Item.Model.IsRead)
                ChangeFavoriteStatusButtonFontIcon = CreateFontIcon(_starredGlyph);
            else
                ChangeFavoriteStatusButtonFontIcon = CreateFontIcon(_unstarredGlyph);
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
            Item = new ItemViewModel(parameter as Item);

            ChangeReadStatusCommand = new DelegateCommand(() => ChangeReadStatus());
            ChangeFavoriteStatusCommand = new DelegateCommand(() => ChangeFavoriteStatus());

            UpdateReadIcon();
            UpdateFavoriteIcon();
            UpdateBrushes();

            await GenerateFormattedHtmlAsync();
        }
    }
}

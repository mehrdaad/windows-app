using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Common;
using wallabag.Models;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class ItemPageViewModel : ViewModelBase
    {
        public ItemViewModel Item { get; set; }

        public string FormattedHtml { get; set; }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            Item = new ItemViewModel(parameter as Item);
            await GenerateFormattedHtmlAsync();
        }

        private async Task GenerateFormattedHtmlAsync()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Article/article.html"));
            string _template = await FileIO.ReadTextAsync(file);

            string accentColor = Application.Current.Resources["SystemAccentColor"].ToString().Remove(1, 2);
            StringBuilder styleSheetBuilder = new StringBuilder();
            styleSheetBuilder.Append("<style>");
            styleSheetBuilder.Append("hr {border-color: " + accentColor + " !important}");
            styleSheetBuilder.Append("::selection,mark {background: " + accentColor + " !important}");
            //styleSheetBuilder.Append("body {");
            //styleSheetBuilder.Append($"font-size:{AppSettings.FontSize}px;");
            //styleSheetBuilder.Append("text-align: " + AppSettings.TextAlignment + "}");
            styleSheetBuilder.Append("</style>");

            FormattedHtml = _template.FormatWith(new
            {
                title = Item.Model.Title,
                content = Item.Model.Content,
                articleUrl = Item.Model.Url,
                hostname = Item.Model.Hostname,
                color = "sepia",
                font = "serif",
                progress = "50",
                publishDate = string.Format("{0:d}", Item.Model.CreationDate),
                stylesheet = styleSheetBuilder.ToString()
            });
        }        
    }
}

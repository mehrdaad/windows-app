using System;
using System.Linq;
using Template10.Common;
using wallabag.Data.Services;
using static wallabag.Data.Common.Navigation;

namespace wallabag.Services
{
    class NavigationService : INavigationService
    {
        public string CurrentPageKey
        {
            get
            {
                var currentPageType = BootStrapper.Current.NavigationService.CurrentPageType;
                var dict = BootStrapper.Current.PageKeys<Pages>();

                if (dict.ContainsValue(currentPageType))
                    return dict.First(t => t.Value.FullName == currentPageType.FullName).Key.ToString();
                else
                    return "-EMPTY-";
            }

        }

        public void ClearHistory()
        {
            BootStrapper.Current.NavigationService.ClearHistory();
            BootStrapper.Current.NavigationService.ClearCache();
        }

        public void GoBack() => BootStrapper.Current.NavigationService.GoBack();
        public void Navigate(Pages pageKey) => HandleNavigation(pageKey);
        public void Navigate(Pages pageKey, object parameter) => HandleNavigation(pageKey, parameter);

        // TODO: Remove this piece of shit! NO ASYNC VOIDS! NEVER!
        private async void HandleNavigation(Pages pageKey, object parameter = null)
        {
            if (pageKey == Pages.AddItemPage)
                await new Dialogs.AddItemDialog().ShowAsync();
            else if (pageKey == Pages.EditTagsPage)
                await new Dialogs.EditTagsDialog().ShowAsync();
            else
                BootStrapper.Current.NavigationService.Navigate(pageKey, parameter);
        }

        public void Configure(Pages pageKey, Type pageType)
        {
            var keys = BootStrapper.Current.PageKeys<Pages>();
            keys.Add(pageKey, pageType);
        }

        [Obsolete("Please use the Pages enumeration instead.", true)]
        public void NavigateTo(string pageKey) => throw new NotImplementedException();

        [Obsolete("Please use the Pages enumeration instead.", true)]
        public void NavigateTo(string pageKey, object parameter) => throw new NotImplementedException();
    }
}

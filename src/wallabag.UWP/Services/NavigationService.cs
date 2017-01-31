using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Template10.Common;
using wallabag.Data.Common;
using wallabag.Data.Services;
using Windows.UI.Xaml.Controls;
using static wallabag.Data.Common.Navigation;

namespace wallabag.Services
{
    class NavigationService : INavigationService
    {
        private ILoggingService _loggingService => SimpleIoc.Default.GetInstance<ILoggingService>();

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
        public void Navigate(Pages pageKey) => Navigate(GetPageType(pageKey));
        public void Navigate(Pages pageKey, object parameter) => Navigate(GetPageType(pageKey), parameter);
        public void Navigate(Type page) => Navigate(page, null);
        public void Navigate(Type page, object parameter) => HandleNavigationAsync(page, parameter).ConfigureAwait(true);

        public void Configure(Pages pageKey, Type pageType)
        {
            var keys = BootStrapper.Current.PageKeys<Pages>();
            keys.Add(pageKey, pageType);
        }

        private async Task HandleNavigationAsync(Type pageType, object parameter = null)
        {
            if (pageType == GetPageType(Pages.AddItemPage))
                await new Dialogs.AddItemDialog().ShowAsync();
            else if (pageType == GetPageType(Pages.EditTagsPage))
                await new Dialogs.EditTagsDialog().ShowAsync();
            else
            {
                var ns = BootStrapper.Current.NavigationService;
                _loggingService.WriteLine($"Navigating to {pageType}. Type of parameter: {parameter?.GetType()?.Name}");

                var pageState = new Dictionary<string, object>();

                var oldPage = ns.FrameFacade.Content as Page;
                var oldViewModel = oldPage?.DataContext as INavigable;

                _loggingService.WriteLine("Starting navigation...");
                ns.FrameFacade.Navigate(pageType, parameter, null);

                if (oldViewModel != null)
                {
                    _loggingService.WriteLine($"Executing {nameof(INavigable.OnNavigatedFromAsync)} from old ViewModel ({oldViewModel?.GetType()?.Name ?? "NULL"}).");
                    await oldViewModel.OnNavigatedFromAsync(pageState);
                }
                else _loggingService.WriteLine($"{nameof(INavigable.OnNavigatedFromAsync)} wasn't executed because the ViewModel was null.");

                // fetch (current which is now new)
                var newPage = ns.FrameFacade.Content as Page;
                var newViewModel = newPage?.DataContext as INavigable;

                if (newViewModel != null)
                {
                    _loggingService.WriteLine($"Executing {nameof(INavigable.OnNavigatedToAsync)} from new ViewModel ({newViewModel?.GetType()?.Name ?? "NULL"}).");
                    await newViewModel.OnNavigatedToAsync(parameter, pageState);
                }
                else _loggingService.WriteLine($"{nameof(INavigable.OnNavigatedToAsync)} wasn't executed because the ViewModel was null.");
            }
        }
        private Type GetPageType(Pages pageKey) => BootStrapper.Current.PageKeys<Pages>().First(i => i.Key == pageKey).Value;

        [Obsolete("Please use the Pages enumeration instead.", true)]
        public void NavigateTo(string pageKey) => throw new NotImplementedException();

        [Obsolete("Please use the Pages enumeration instead.", true)]
        public void NavigateTo(string pageKey, object parameter) => throw new NotImplementedException();
    }
}

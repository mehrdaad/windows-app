using System;

namespace wallabag.Data.Services
{
    public interface INavigationService : GalaSoft.MvvmLight.Views.INavigationService
    {
        void Configure(Common.Navigation.Pages page, Type pageType);

        void Navigate(Common.Navigation.Pages page);
        void Navigate(Common.Navigation.Pages page, object parameter);
        void ClearHistory();
    }
}

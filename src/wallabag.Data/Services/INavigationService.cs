using System;
using System.Threading.Tasks;

namespace wallabag.Data.Services
{
    public interface INavigationService
    {
        void Configure(Common.Navigation.Pages page, Type pageType);

        void Navigate(Common.Navigation.Pages page);
        void Navigate(Common.Navigation.Pages page, object parameter);
        void Navigate(Type page);
        void Navigate(Type page, object parameter);

        void GoBack();
        void ClearHistory();
        Task SaveAsync();
        void Resume();
    }
}

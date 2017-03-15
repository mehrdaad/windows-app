using System;
using System.Threading.Tasks;

namespace wallabag.Data.Services
{
    public interface INavigationService
    {
        void Configure(Common.Navigation.Pages page, Type pageType);

        void Navigate(Common.Navigation.Pages page);
        void Navigate(Common.Navigation.Pages page, object parameter);

        void GoBack();
        void ClearHistory();
        Task SaveAsync();
        Task ResumeAsync();
    }
}

using GalaSoft.MvvmLight.Ioc;
using wallabag.Data.Services;

namespace wallabag.Services
{
    class NavigationService : GalaSoft.MvvmLight.Views.NavigationService, INavigationService
    {
        public void ClearHistory()
        {
            var l = SimpleIoc.Default.GetInstance<ILoggingService>();
            l.WriteLine("ClearHistory() is not implemented yet!", LoggingCategory.Critical);
        }
    }
}

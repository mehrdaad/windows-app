using GalaSoft.MvvmLight.Ioc;
using SQLite.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Data.Common;
using wallabag.Data.Services;

namespace wallabag.Data.ViewModels
{
    public class ViewModelBase : GalaSoft.MvvmLight.ViewModelBase, INavigable
    {
        internal IWallabagClient _client => SimpleIoc.Default.GetInstance<IWallabagClient>();
        internal static SQLiteConnection _database => SimpleIoc.Default.GetInstance<SQLiteConnection>();
        internal INavigationService _navigationService => SimpleIoc.Default.GetInstance<INavigationService>();
        internal ILoggingService _loggingService => SimpleIoc.Default.GetInstance<ILoggingService>();

        internal IDictionary<string, object> SessionState => SimpleIoc.Default.GetInstance<Dictionary<string, object>>("SessionState");

        public virtual Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state)
        {
            return Task.CompletedTask;
        }
        public virtual Task OnNavigatedFromAsync(IDictionary<string, object> pageState)
        {
            base.Cleanup();
            return Task.CompletedTask;
        }
    }
}

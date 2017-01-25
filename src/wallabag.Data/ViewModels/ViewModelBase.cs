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
        internal IWallabagClient Client => SimpleIoc.Default.GetInstance<IWallabagClient>();
        internal static SQLiteConnection Database => SimpleIoc.Default.GetInstance<SQLiteConnection>();
        internal INavigationService Navigation => SimpleIoc.Default.GetInstance<INavigationService>();
        internal IDialogService DialogService => SimpleIoc.Default.GetInstance<IDialogService>();
        internal ILoggingService LoggingService => SimpleIoc.Default.GetInstance<ILoggingService>();

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

using GalaSoft.MvvmLight.Views;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Data.Common;

namespace wallabag.Data.ViewModels
{
    public class ViewModelBase : GalaSoft.MvvmLight.ViewModelBase, INavigable
    {
        internal IWallabagClient Client;
        internal static SQLite.Net.SQLiteConnection Database;
        internal INavigationService Navigation;
        internal IDialogService DialogService;

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

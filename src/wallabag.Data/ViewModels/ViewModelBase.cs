using GalaSoft.MvvmLight.Views;
using wallabag.Api;

namespace wallabag.Data.ViewModels
{
    public class ViewModelBase : GalaSoft.MvvmLight.ViewModelBase
    {
        internal IWallabagClient Client;
        internal SQLite.Net.SQLiteConnection Database;
        internal INavigationService Navigation;
    }
}

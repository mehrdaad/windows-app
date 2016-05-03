using System;
using System.Threading.Tasks;
using Template10.Common;
using Windows.ApplicationModel.Activation;
using wallabag.Api.Models;

namespace wallabag
{
    public sealed partial class App : BootStrapper
    {
        public static Api.WallabagClient Client { get; private set; }
        public static SQLite.SQLiteAsyncConnection Database { get; private set; }

        public App() { InitializeComponent(); }

        public override async Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            if (startKind == StartKind.Launch)
            {
                Database = new SQLite.SQLiteAsyncConnection("wallabag.db", true);
                await Database.CreateTableAsync<WallabagItem>();
                await Database.CreateTableAsync<WallabagTag>();

                NavigationService.Navigate(typeof(Views.MainPage));
            }
        }
    }
}

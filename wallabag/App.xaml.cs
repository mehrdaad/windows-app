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
                Client = new Api.WallabagClient(new Uri("https://wallabag.jlnostr.de"), "1_4xy4khl22uck0o8gcs4kw4cwg80s88os0kw0k4so4ssg804ogk", "5wakw2t7uxkwsww480swooow8gsgko8w40wso0w8c4gocsc4ws");

                Database = new SQLite.SQLiteAsyncConnection("wallabag.db", true);
                await Database.CreateTableAsync<WallabagItem>();
                await Database.CreateTableAsync<WallabagTag>();

                NavigationService.Navigate(typeof(Views.MainPage));
            }
        }
    }
}

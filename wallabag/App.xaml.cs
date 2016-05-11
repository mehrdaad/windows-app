using Newtonsoft.Json;
using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System;
using System.Threading.Tasks;
using Template10.Common;
using wallabag.Api.Models;
using wallabag.Services;
using Windows.ApplicationModel.Activation;

namespace wallabag
{
    public sealed partial class App : BootStrapper
    {
        public static Api.WallabagClient Client { get; private set; }
        public static SQLiteConnection Database { get; private set; }
        public static SettingsService Settings { get; private set; }

        public App() { InitializeComponent(); }

        public override async Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            if (startKind == StartKind.Launch)
            {
                Settings = SettingsService.Instance;
                Client = new Api.WallabagClient(Settings.WallabagUrl, Settings.ClientId, Settings.ClientSecret);

                var path = (await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFileAsync("wallabag.db", Windows.Storage.CreationCollisionOption.ReplaceExisting)).Path;

                await Task.Factory.StartNew(() =>
                {
                    Database = new SQLiteConnection(new SQLitePlatformWinRT(), path, serializer: new JsonSerializer());
                    Database.CreateTable<WallabagItem>();
                    Database.CreateTable<WallabagTag>();
                });

                NavigationService.Navigate(typeof(Views.MainPage));
            }
        }

        public class JsonSerializer : IBlobSerializer
        {
            public bool CanDeserialize(Type type) => true;

            public object Deserialize(byte[] data, Type type)
            {
                var str = System.Text.Encoding.UTF8.GetString(data);
                return JsonConvert.DeserializeObject(str);
            }

            public byte[] Serialize<T>(T obj)
            {
                return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
            }
        }
    }
}

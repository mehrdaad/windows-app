﻿using Newtonsoft.Json;
using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System;
using System.Threading.Tasks;
using Template10.Common;
using wallabag.Models;
using wallabag.Services;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel;
using System.IO;

namespace wallabag
{
    public sealed partial class App : BootStrapper
    {
        public static Api.WallabagClient Client { get; private set; }
        public static SQLiteConnection Database { get; private set; }
        public static SettingsService Settings { get; private set; }

        public static EventHandler OfflineTasksChanged;

        public App() { InitializeComponent(); }

        public override Task OnInitializeAsync(IActivatedEventArgs args)
        {
            Settings = SettingsService.Instance;

            if (Settings.AllowCollectionOfTelemetryData)
                Microsoft.HockeyApp.HockeyClient.Current.Configure("842955f8fd3b4191972db776265d81c4");

            CreateClientAndDatabase();

            Client.CredentialsRefreshed += (s, e) =>
            {
                Settings.ClientId = Client.ClientId;
                Settings.ClientSecret = Client.ClientSecret;
                Settings.AccessToken = Client.AccessToken;
                Settings.RefreshToken = Client.RefreshToken;
                Settings.LastTokenRefreshDateTime = Client.LastTokenRefreshDateTime;
            };

            Client.AccessToken = Settings.AccessToken;
            Client.RefreshToken = Settings.RefreshToken;
            Client.LastTokenRefreshDateTime = Settings.LastTokenRefreshDateTime;

            return Task.CompletedTask;
        }

        public override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.ShareTarget)
            {
                SessionState["shareTarget"] = args;
                NavigationService.Navigate(typeof(Views.ShareTargetPage));
            }
            else
            {
                if (string.IsNullOrEmpty(Settings.AccessToken) || string.IsNullOrEmpty(Settings.RefreshToken))
                {
                    Client = new Api.WallabagClient(null, string.Empty, string.Empty);
                    NavigationService.Navigate(typeof(Views.LoginPage));
                }
                else if (Database.Table<Item>().Count() == 0)
                {
                    Client.AccessToken = Settings.AccessToken;
                    Client.RefreshToken = Settings.RefreshToken;
                    Client.LastTokenRefreshDateTime = Settings.LastTokenRefreshDateTime;

                    NavigationService.Navigate(typeof(Views.LoginPage), true);
                }
                else
                    NavigationService.Navigate(typeof(Views.MainPage));
            }
            return Task.CompletedTask;
        }

        public override Task OnSuspendingAsync(object s, SuspendingEventArgs e, bool prelaunchActivated)
        {
            return NavigationService.SaveNavigationAsync();
        }

        public override async void OnResuming(object s, object e, AppExecutionState previousExecutionState)
        {
            Client.AccessToken = Settings.AccessToken;
            Client.RefreshToken = Settings.RefreshToken;
            Client.LastTokenRefreshDateTime = Settings.LastTokenRefreshDateTime;

            if (previousExecutionState == AppExecutionState.Suspended)
                await NavigationService.RestoreSavedNavigationAsync();
        }

        private void CreateClientAndDatabase()
        {
            if (Client == null)
                Client = new Api.WallabagClient(Settings.WallabagUrl, Settings.ClientId, Settings.ClientSecret);

            if (Database == null)
            {
                var path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, "wallabag.db");

                Database = new SQLiteConnection(new SQLitePlatformWinRT(), path, serializer: new CustomBlobSerializer());
                Database.CreateTable<Item>();
                Database.CreateTable<Tag>();
                Database.CreateTable<OfflineTask>();
            }
        }
    }

    public class CustomBlobSerializer : IBlobSerializer
    {
        private JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full
        };

        public bool CanDeserialize(Type type) => true;

        public object Deserialize(byte[] data, Type type)
        {
            var str = System.Text.Encoding.UTF8.GetString(data);

            if (type == typeof(Uri))
                return new Uri(str.Replace("\"", string.Empty));
            else
                return JsonConvert.DeserializeObject(str, _serializerSettings);
        }

        public byte[] Serialize<T>(T obj)
        {
            if (typeof(T) == typeof(Uri))
                return System.Text.Encoding.UTF8.GetBytes(obj.ToString());
            else
                return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, _serializerSettings));
        }
    }
}
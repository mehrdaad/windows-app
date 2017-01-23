using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System.IO;
using wallabag.Api;
using wallabag.Data.Common;
using wallabag.Data.Models;

namespace wallabag.Data.ViewModels
{
    class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<IWallabagClient>(() =>
            {
                var client = new WallabagClient(Settings.Authentication.WallabagUri, Settings.Authentication.ClientId, Settings.Authentication.ClientSecret);

                if (!string.IsNullOrEmpty(Settings.Authentication.AccessToken) &&
                    !string.IsNullOrEmpty(Settings.Authentication.RefreshToken))
                {
                    client.AccessToken = Settings.Authentication.AccessToken;
                    client.RefreshToken = Settings.Authentication.RefreshToken;
                    client.LastTokenRefreshDateTime = Settings.Authentication.LastTokenRefreshDateTime;
                }

                client.CredentialsRefreshed += (s, e) =>
                {
                    Settings.Authentication.ClientId = client.ClientId;
                    Settings.Authentication.ClientSecret = client.ClientSecret;
                    Settings.Authentication.AccessToken = client.AccessToken;
                    Settings.Authentication.RefreshToken = client.RefreshToken;
                    Settings.Authentication.LastTokenRefreshDateTime = client.LastTokenRefreshDateTime;
                };

                return client;
            });
            SimpleIoc.Default.Register<SQLiteConnection>(() =>
            {
                string path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, "wallabag.db");

                var db = new SQLiteConnection(new SQLitePlatformWinRT(), path, serializer: new CustomBlobSerializer());
                db.CreateTable<Item>();
                db.CreateTable<Tag>();
                db.CreateTable<OfflineTask>();

                return db;
            });

            SimpleIoc.Default.Register<MainViewModel>();
        }

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();
    }
}

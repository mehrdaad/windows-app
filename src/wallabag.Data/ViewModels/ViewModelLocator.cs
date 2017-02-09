using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using SQLite.Net;
using System.Collections.Generic;
using System.IO;
using wallabag.Api;
using wallabag.Data.Common;
using wallabag.Data.Models;

namespace wallabag.Data.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            if (GalaSoft.MvvmLight.ViewModelBase.IsInDesignModeStatic)
                return;

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
                var device = SimpleIoc.Default.GetInstance<Interfaces.IPlatformSpecific>();

                var db = new SQLiteConnection(device.GetSQLitePlatform(), device.DatabasePath, serializer: new CustomBlobSerializer());
                db.CreateTable<Item>();
                db.CreateTable<Tag>();
                db.CreateTable<OfflineTask>();

                return db;
            });
            SimpleIoc.Default.Register(() => new Dictionary<string, object>(), "SessionState");

            SimpleIoc.Default.Register<AddItemViewModel>();
            SimpleIoc.Default.Register<EditTagsViewModel>();
            SimpleIoc.Default.Register<ItemPageViewModel>();
            SimpleIoc.Default.Register<LoginPageViewModel>();
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<SettingsPageViewModel>();
        }

        public AddItemViewModel AddItem => ServiceLocator.Current.GetInstance<AddItemViewModel>();
        public EditTagsViewModel EditTags => ServiceLocator.Current.GetInstance<EditTagsViewModel>();
        public ItemPageViewModel ItemView => ServiceLocator.Current.GetInstance<ItemPageViewModel>();
        public LoginPageViewModel Login => ServiceLocator.Current.GetInstance<LoginPageViewModel>();
        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();
        public SettingsPageViewModel SettingsView => ServiceLocator.Current.GetInstance<SettingsPageViewModel>();
    }
}

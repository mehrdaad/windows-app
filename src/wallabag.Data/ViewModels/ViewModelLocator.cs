﻿using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using SQLite.Net;
using System.Collections.Generic;
using wallabag.Api;
using wallabag.Data.Common;
using wallabag.Data.Models;
using wallabag.Data.Services;

namespace wallabag.Data.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            if (GalaSoft.MvvmLight.ViewModelBase.IsInDesignModeStatic)
                return;

            if (!SimpleIoc.Default.IsRegistered<IWallabagClient>())
            {
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

                    var db = new SQLiteConnection(device.GetSQLitePlatform(), device.GetDatabasePath(), serializer: new CustomBlobSerializer());
                    db.CreateTable<Item>();
                    db.CreateTable<Tag>();
                    db.CreateTable<OfflineTask>();

                    return db;
                });
                SimpleIoc.Default.Register(() => new Dictionary<string, object>(), "SessionState");

                // TODO: Re-enable the generic ApiClientCreationService if cookie issue is done.
                //SimpleIoc.Default.Register<IApiClientCreationService, ApiClientCreationService>();
                SimpleIoc.Default.Register<IOfflineTaskService, OfflineTaskService>();

                SimpleIoc.Default.Register<AddItemViewModel>();
                SimpleIoc.Default.Register<EditTagsViewModel>();
                SimpleIoc.Default.Register<ItemPageViewModel>();
                SimpleIoc.Default.Register<LoginPageViewModel>();
                SimpleIoc.Default.Register<MainViewModel>();
                SimpleIoc.Default.Register<SettingsPageViewModel>();
                SimpleIoc.Default.Register<MultipleSelectionViewModel>();
                SimpleIoc.Default.Register<QRScanPageViewModel>();
            }
        }

        public AddItemViewModel AddItem => SimpleIoc.Default.GetInstanceWithoutCaching<AddItemViewModel>();
        public EditTagsViewModel EditTags => SimpleIoc.Default.GetInstanceWithoutCaching<EditTagsViewModel>();
        public ItemPageViewModel ItemView => SimpleIoc.Default.GetInstanceWithoutCaching<ItemPageViewModel>();
        public LoginPageViewModel Login => SimpleIoc.Default.GetInstance<LoginPageViewModel>();
        public MainViewModel Main => SimpleIoc.Default.GetInstance<MainViewModel>();
        public SettingsPageViewModel SettingsView => SimpleIoc.Default.GetInstance<SettingsPageViewModel>();
        public QRScanPageViewModel QRScan => SimpleIoc.Default.GetInstance<QRScanPageViewModel>();
    }
}

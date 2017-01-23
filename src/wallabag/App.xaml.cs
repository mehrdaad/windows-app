using GalaSoft.MvvmLight.Ioc;
using Microsoft.HockeyApp;
using Newtonsoft.Json;
using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Template10.Common;
using wallabag.Common.Helpers;
using wallabag.Data.Common;
using wallabag.Data.Services;
using wallabag.Models;
using wallabag.Services;
using wallabag.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;

namespace wallabag
{
    public sealed partial class App : BootStrapper
    {
        public static Api.WallabagClient Client { get; private set; }
        public static SQLiteConnection Database { get; private set; }
        public static SettingsService Settings { get { return SettingsService.Instance; } }

        public App() { InitializeComponent(); }

        public override Task OnInitializeAsync(IActivatedEventArgs args)
        {
#if DEBUG == false
            if (Settings.AllowCollectionOfTelemetryData)
                HockeyClient.Current.Configure("842955f8fd3b4191972db776265d81c4");
#endif

            SimpleIoc.Default.Register<IDialogService, DialogService>();
            SimpleIoc.Default.Register<IBackgroundTaskService, BackgroundTaskService>();
            SimpleIoc.Default.Register<ILoggingService, LoggingService>();
            SimpleIoc.Default.Register<ISettingsService, SettingsService>();
            SimpleIoc.Default.Register<INavigationService>(() =>
            {
                var ns = new NavigationService();

                ns.Configure(Pages.ItemPage, typeof(ItemPage));
                ns.Configure(Pages.LoginPage, typeof(LoginPage));
                ns.Configure(Pages.MainPage, typeof(MainPage));
                ns.Configure(Pages.QRScanPage, typeof(QRScanPage));
                ns.Configure(Pages.SettingsPage, typeof(SettingsPage));

                return ns;
            });

            CreateClientAndDatabase();

            Client.CredentialsRefreshed += (s, e) =>
            {
                Settings.ClientId = Client.ClientId;
                Settings.ClientSecret = Client.ClientSecret;
                Settings.AccessToken = Client.AccessToken;
                Settings.RefreshToken = Client.RefreshToken;
                Settings.LastTokenRefreshDateTime = Client.LastTokenRefreshDateTime;
            };

            if (Settings.BackgroundTaskIsEnabled && BackgroundTaskHelper.BackgroundTaskIsRegistered == false)
                return BackgroundTaskHelper.RegisterBackgroundTaskAsync();

            return Task.CompletedTask;
        }

        public override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.ShareTarget)
            {
                SessionState["shareTarget"] = args;
                NavigationService.Navigate(typeof(Views.ShareTargetPage));
            }
            else if (args.Kind == ActivationKind.Protocol)
            {
                var a = args as ProtocolActivatedEventArgs;
                var protocolParameter = ProtocolHelper.Parse(a.Uri.ToString());

                if (protocolParameter != null && protocolParameter.Server.IsValidUri())
                {
                    NavigationService.Navigate(typeof(Views.LoginPage), protocolParameter);
                    NavigationService.ClearCache();
                    NavigationService.ClearHistory();
                }
            }
            else
            {
                if (string.IsNullOrEmpty(Settings.AccessToken) || string.IsNullOrEmpty(Settings.RefreshToken))
                {
                    Client = new Api.WallabagClient(null, string.Empty, string.Empty);
                    NavigationService.Navigate(typeof(Views.LoginPage));
                }
                else
                    NavigationService.Navigate(typeof(Views.MainPage));
            }
            return Task.CompletedTask;
        }

        public override Task OnSuspendingAsync(object s, SuspendingEventArgs e, bool prelaunchActivated)
        {
            return NavigationService?.SaveAsync();
        }

        public override void OnResuming(object s, object e, AppExecutionState previousExecutionState)
        {
            Client.AccessToken = Settings.AccessToken;
            Client.RefreshToken = Settings.RefreshToken;
            Client.LastTokenRefreshDateTime = Settings.LastTokenRefreshDateTime;

            if (previousExecutionState == AppExecutionState.Suspended)
                NavigationService.Resuming();
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            CreateClientAndDatabase();
            var deferral = args.TaskInstance.GetDeferral();

            await OfflineTaskService.ExecuteAllAsync();

            if (SettingsService.Instance.DownloadNewItemsDuringExecutionOfBackgroundTask)
            {
                var items = await Client.GetItemsAsync(
                    dateOrder: Api.WallabagClient.WallabagDateOrder.ByLastModificationDate,
                    sortOrder: Api.WallabagClient.WallabagSortOrder.Descending,
                    since: SettingsService.Instance.LastSuccessfulSyncDateTime);

                if (items != null)
                {
                    var itemList = new List<Item>();

                    foreach (var item in items)
                        itemList.Add(item);

                    var databaseList = Database.Query<Item>($"SELECT Id FROM Item ORDER BY LastModificationDate DESC LIMIT 0,{itemList.Count}", Array.Empty<object>());
                    var deletedItems = databaseList.Except(itemList);

                    Database.RunInTransaction(() =>
                    {
                        foreach (var item in deletedItems)
                            Database.Delete(item);

                        Database.InsertOrReplaceAll(itemList);
                    });

                    SettingsService.Instance.LastSuccessfulSyncDateTime = DateTime.Now;
                }
            }

            SettingsService.Instance.LastExecutionOfBackgroundTask = DateTime.Now;
            deferral.Complete();
        }

        private void CreateClientAndDatabase()
        {
            if (Client == null)
                Client = new Api.WallabagClient(Settings.WallabagUrl, Settings.ClientId, Settings.ClientSecret);

            if (!string.IsNullOrEmpty(Settings.AccessToken) &&
                !string.IsNullOrEmpty(Settings.RefreshToken))
            {
                Client.AccessToken = Settings.AccessToken;
                Client.RefreshToken = Settings.RefreshToken;
                Client.LastTokenRefreshDateTime = Settings.LastTokenRefreshDateTime;
            }

            if (Database == null)
            {
                string path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, "wallabag.db");

                Database = new SQLiteConnection(new SQLitePlatformWinRT(), path, serializer: new CustomBlobSerializer());
                Database.CreateTable<Item>();
                Database.CreateTable<Tag>();
                Database.CreateTable<OfflineTask>();
            }
        }
    }


        }
    }
}

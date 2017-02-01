using GalaSoft.MvvmLight.Ioc;
using Microsoft.HockeyApp;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Template10.Common;
using wallabag.Api;
using wallabag.Data.Common;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Models;
using wallabag.Data.Services;
using wallabag.Services;
using wallabag.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using static wallabag.Data.Common.Navigation;

namespace wallabag
{
    public sealed partial class App : BootStrapper
    {
        private IWallabagClient _client => SimpleIoc.Default.GetInstance<IWallabagClient>();
        private SQLiteConnection _database => SimpleIoc.Default.GetInstance<SQLiteConnection>();
        private new INavigationService NavigationService => SimpleIoc.Default.GetInstance<INavigationService>();

        public App() { InitializeComponent(); }

        public override Task OnInitializeAsync(IActivatedEventArgs args)
        {
#if DEBUG == false
            if (Settings.General.AllowCollectionOfTelemetryData)
                HockeyClient.Current.Configure("842955f8fd3b4191972db776265d81c4");
#endif

            RegisterServices();
            return EnsureRegistrationOfBackgroundTaskAsync();
        }
        public override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.ShareTarget)
            {
                SessionState["shareTarget"] = args;
                NavigationService.Navigate(typeof(ShareTargetPage));
            }
            else if (args.Kind == ActivationKind.Protocol)
            {
                var a = args as ProtocolActivatedEventArgs;
                var protocolParameter = ProtocolHelper.Parse(a.Uri.ToString());

                if (protocolParameter != null && protocolParameter.Server.IsValidUri())
                {
                    NavigationService.Navigate(Pages.LoginPage, protocolParameter);
                    NavigationService.ClearHistory();
                }
            }
            else
            {
                if (string.IsNullOrEmpty(Settings.Authentication.AccessToken) || string.IsNullOrEmpty(Settings.Authentication.RefreshToken))
                    NavigationService.Navigate(Pages.LoginPage);
                else
                    NavigationService.Navigate(Pages.MainPage);
            }
            return Task.CompletedTask;
        }

        public override Task OnSuspendingAsync(object s, SuspendingEventArgs e, bool prelaunchActivated)
        {
            return NavigationService?.SaveAsync();
        }

        public override void OnResuming(object s, object e, AppExecutionState previousExecutionState)
        {
            if (previousExecutionState == AppExecutionState.Suspended)
                NavigationService.Resume();
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            RegisterServices();
            var deferral = args.TaskInstance.GetDeferral();

            await SimpleIoc.Default.GetInstance<IOfflineTaskService>().ExecuteAllAsync();

            if (Settings.BackgroundTask.DownloadNewItemsDuringExecution)
            {
                var items = await _client.GetItemsAsync(
                    dateOrder: WallabagClient.WallabagDateOrder.ByLastModificationDate,
                    sortOrder: WallabagClient.WallabagSortOrder.Descending,
                    since: Settings.General.LastSuccessfulSyncDateTime);

                if (items != null)
                {
                    var itemList = new List<Item>();

                    foreach (var item in items)
                        itemList.Add(item);

                    var databaseList = _database.Query<Item>($"SELECT Id FROM Item ORDER BY LastModificationDate DESC LIMIT 0,{itemList.Count}", Array.Empty<object>());
                    var deletedItems = databaseList.Except(itemList);

                    _database.RunInTransaction(() =>
                    {
                        foreach (var item in deletedItems)
                            _database.Delete(item);

                        _database.InsertOrReplaceAll(itemList);
                    });

                    Settings.General.LastSuccessfulSyncDateTime = DateTime.Now;
                }
            }

            Settings.BackgroundTask.LastExecution = DateTime.Now;
            deferral.Complete();
        }

        private void RegisterServices()
        {
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
            SimpleIoc.Default.Register<IOfflineTaskService, OfflineTaskService>();
        }
        private Task EnsureRegistrationOfBackgroundTaskAsync()
        {
            var bts = SimpleIoc.Default.GetInstance<IBackgroundTaskService>();

            if (Settings.BackgroundTask.IsEnabled &&
                bts.IsRegistered == false && bts.IsSupported)
                return bts.RegisterBackgroundTaskAsync();
            else
                return Task.CompletedTask;
        }
    }
}
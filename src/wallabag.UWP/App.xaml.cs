using GalaSoft.MvvmLight.Ioc;
using Microsoft.HockeyApp;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Data.Common;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using wallabag.Data.Services.OfflineTaskService;
using wallabag.Services;
using wallabag.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using static wallabag.Data.Common.Navigation;

namespace wallabag
{
    public sealed partial class App : Application
    {
        private bool _firstActivationExecuted;

        private IWallabagClient _client => SimpleIoc.Default.GetInstance<IWallabagClient>();
        private SQLiteConnection _database => SimpleIoc.Default.GetInstance<SQLiteConnection>();
        private INavigationService _navigation => SimpleIoc.Default.GetInstance<INavigationService>();

        public Dictionary<string, object> SessionState => SimpleIoc.Default.GetInstance<Dictionary<string, object>>("SessionState");

        public App() => InitializeComponent();

        public enum StartKind { Launch, Activate }
        public enum AppExecutionState { Suspended, Terminated, Prelaunch }

        public async Task OnInitializeAsync(IActivatedEventArgs args)
        {
            RegisterServices();

            // Run possible migrations here

            // Reset the application after update to separated app model
            if (string.IsNullOrEmpty(Settings.General.AppVersion))
            {
                var device = SimpleIoc.Default.GetInstance<IPlatformSpecific>();
                await device.DeleteDatabaseAsync();

                Settings.Authentication.AccessToken = string.Empty;
                Settings.Authentication.RefreshToken = string.Empty;

                Settings.General.AppVersion = SimpleIoc.Default.GetInstance<IPlatformSpecific>().AppVersion;
                device.CloseApplication();
            }

            Settings.General.AppVersion = SimpleIoc.Default.GetInstance<IPlatformSpecific>().AppVersion;

            if (!System.Diagnostics.Debugger.IsAttached && Settings.General.AllowCollectionOfTelemetryData)
                HockeyClient.Current
                    .Configure("842955f8fd3b4191972db776265d81c4")
                    .SetExceptionDescriptionLoader(ex =>
                   {
                       var file = ApplicationData.Current.TemporaryFolder.CreateFileAsync("log.txt", CreationCollisionOption.OpenIfExists).AsTask().Result;
                       var lines = FileIO.ReadLinesAsync(file).AsTask().Result;

                       int numberOfLogEntries = 250;

                       return string.Join("\r\n", lines.Skip(Math.Max(0, lines.Count - numberOfLogEntries)));
                   });

            App.Current.UnhandledException += async (s, e) => await SaveLogsToFile();

            await EnsureRegistrationOfBackgroundTaskAsync();
        }
        public Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.ShareTarget)
            {
                var frame = new Frame();
                frame.Navigate(typeof(ShareTargetPage), (args as ShareTargetActivatedEventArgs).ShareOperation);
                Window.Current.Content = frame;
            }
            else if (args.Kind == ActivationKind.Protocol)
            {
                var a = args as ProtocolActivatedEventArgs;
                var protocolParameter = ProtocolHelper.Parse(a.Uri.ToString());

                if (protocolParameter != null && protocolParameter.Server.IsValidUri())
                {
                    _navigation.Navigate(Pages.LoginPage, protocolParameter);
                    _navigation.ClearHistory();
                }
            }
            else
            {
                if (string.IsNullOrEmpty(Settings.Authentication.AccessToken) || string.IsNullOrEmpty(Settings.Authentication.RefreshToken))
                    _navigation.Navigate(Pages.LoginPage);
                else
                    _navigation.Navigate(Pages.MainPage);
            }
            return Task.CompletedTask;
        }

        public async Task OnSuspendingAsync(object s, SuspendingEventArgs e, bool prelaunchActivated)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            await _navigation?.SaveAsync();
            await SaveLogsToFile();

            deferral.Complete();
        }
        public async void OnResuming(object s, object e, AppExecutionState previousExecutionState)
        {
            if (previousExecutionState == AppExecutionState.Suspended)
                await _navigation.ResumeAsync();
        }

        public async Task SaveLogsToFile()
        {
            string log = SimpleIoc.Default.GetInstance<ILoggingService>().Log;
            var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("log.txt", CreationCollisionOption.OpenIfExists);

            await FileIO.WriteTextAsync(file, log);
        }

        #region Application setup

        private void RegisterServices()
        {
            if (!SimpleIoc.Default.IsRegistered<ILoggingService>())
            {
                SimpleIoc.Default.Register<ILoggingService, LoggingService>();
                SimpleIoc.Default.Register<IBackgroundTaskService, BackgroundTaskService>();
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
                SimpleIoc.Default.Register<IPlatformSpecific, Common.PlatformSpecific>();
                SimpleIoc.Default.Register<IApiClientCreationService, Services.ApiClientCreationService>();
            }
        }

        private async void StartupOrchestratorAsync(IActivatedEventArgs e, StartKind kind)
        {
            // check if this is the first activation at all, when we can save PreviousExecutionState and PrelaunchActivated
            if (!_firstActivationExecuted)
                _firstActivationExecuted = true;

            // Validate the StartKind
            if (kind == StartKind.Launch && e.PreviousExecutionState == ApplicationExecutionState.Running)
                kind = StartKind.Activate;
            else if (kind == StartKind.Activate && e.PreviousExecutionState != ApplicationExecutionState.Running)
                kind = StartKind.Launch;

            // handle activate
            if (kind == StartKind.Activate)
            {
                await OnStartAsync(kind, e);
                Window.Current.Activate();
            }

            // handle first-time launch
            else if (kind == StartKind.Launch)
            {
                // do some one-time things
                SetupLifecycleListeners();

                // OnInitializeAsync
                await OnInitializeAsync(e);

                // if there no pre-existing root then generate root
                if (Window.Current.Content == null)
                {
                    var frame = new Frame()
                    {
                        ContentTransitions = new TransitionCollection { new NavigationThemeTransition() }
                    };
                    SystemNavigationManager.GetForCurrentView().BackRequested += GlobalBackRequested;

                    Window.Current.Content = frame;
                }

                // okay, now handle launch
                bool isPrelaunch = (e as LaunchActivatedEventArgs)?.PrelaunchActivated ?? false;

                switch (e.PreviousExecutionState)
                {
                    case ApplicationExecutionState.Suspended:
                    case ApplicationExecutionState.Terminated:
                        OnResuming(this, null, isPrelaunch ? AppExecutionState.Prelaunch : AppExecutionState.Terminated);
                        break;
                }

                await OnStartAsync(StartKind.Launch, e);

                Window.Current.Activate();
            }
        }

        public static void GlobalBackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            SimpleIoc.Default.GetInstance<INavigationService>().GoBack();
        }

        private void SetupLifecycleListeners()
        {
            Resuming += delegate (object s, object e)
            {
                if (_firstActivationExecuted)
                    OnResuming(this, e, AppExecutionState.Suspended);
                else
                    OnResuming(this, e, AppExecutionState.Terminated);

                _firstActivationExecuted = true;
            };
            Suspending += async delegate (object s, SuspendingEventArgs e)
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                try
                {
                    await _navigation.SaveAsync();

                    // application-level
                    await OnSuspendingAsync(this, e, false);
                }
                finally { deferral.Complete(); }
            };
        }

        #endregion

        #region Application overrides

        protected override sealed void OnActivated(IActivatedEventArgs e) => StartupOrchestratorAsync(e, StartKind.Activate);
        protected override sealed void OnCachedFileUpdaterActivated(CachedFileUpdaterActivatedEventArgs e) => StartupOrchestratorAsync(e, StartKind.Activate);
        protected override sealed void OnFileActivated(FileActivatedEventArgs e) => StartupOrchestratorAsync(e, StartKind.Activate);
        protected override sealed void OnFileOpenPickerActivated(FileOpenPickerActivatedEventArgs e) => StartupOrchestratorAsync(e, StartKind.Activate);
        protected override sealed void OnFileSavePickerActivated(FileSavePickerActivatedEventArgs e) => StartupOrchestratorAsync(e, StartKind.Activate);
        protected override sealed void OnSearchActivated(SearchActivatedEventArgs e) => StartupOrchestratorAsync(e, StartKind.Activate);
        protected override sealed void OnShareTargetActivated(ShareTargetActivatedEventArgs e) => StartupOrchestratorAsync(e, StartKind.Activate);
        protected override sealed void OnLaunched(LaunchActivatedEventArgs e) => StartupOrchestratorAsync(e, StartKind.Launch);

        #endregion

        #region Background task

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
        private Task EnsureRegistrationOfBackgroundTaskAsync()
        {
            var bts = SimpleIoc.Default.GetInstance<IBackgroundTaskService>();

            if (Settings.BackgroundTask.IsEnabled &&
                !bts.IsRegistered &&
                bts.IsSupported)
                return bts.RegisterBackgroundTaskAsync();
            else
                return Task.CompletedTask;
        }

        #endregion
    }
}
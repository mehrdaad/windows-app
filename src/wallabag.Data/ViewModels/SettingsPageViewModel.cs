using GalaSoft.MvvmLight.Command;
using PropertyChanged;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using wallabag.Data.Common;
using wallabag.Data.Interfaces;
using wallabag.Data.Services;

namespace wallabag.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class SettingsPageViewModel : ViewModelBase
    {
        private readonly ILoggingService _loggingService;
        private readonly IBackgroundTaskService _backgroundTaskService;
        private readonly IPlatformSpecific _device;
        private readonly SQLiteConnection _database;

        private Uri _documentationUri = new Uri("http://doc.wallabag.org/");
        private Uri _twitterAccountUri = new Uri("https://twitter.com/wallabagapp");
        private Uri _mailUri = new Uri("mailto:jlnostr+wallabag@outlook.de");
        private Uri _githubIssueUri = new Uri("https://github.com/wallabag/windows-app/issues/new");
        private Uri _rateAppUri => _device.RateAppUri;

        public bool SyncOnStartup
        {
            get { return Settings.General.SyncOnStartup; }
            set
            {
                Settings.General.SyncOnStartup = value;
                RaisePropertyChanged();
            }
        }
        public bool AllowCollectionOfTelemetryData
        {
            get { return Settings.General.AllowCollectionOfTelemetryData; }
            set
            {
                Settings.General.AllowCollectionOfTelemetryData = value;
                RaisePropertyChanged();
            }
        }
        public bool NavigateBackAfterReadingAnArticle
        {
            get { return Settings.Reading.NavigateBackAfterReadingAnArticle; }
            set
            {
                Settings.Reading.NavigateBackAfterReadingAnArticle = value;
                RaisePropertyChanged();
            }
        }
        public bool SyncReadingProgress
        {
            get { return Settings.Reading.SyncReadingProgress; }
            set
            {
                Settings.Reading.SyncReadingProgress = value;
                RaisePropertyChanged();
            }
        }
        public string VersionNumber => _device.AppVersion;
        public string VideoOpenModeDescription
        {
            get
            {
                switch (Settings.Reading.VideoOpenMode)
                {
                    case Settings.Reading.WallabagVideoOpenMode.Browser:
                        return _device.GetLocalizedResource("VideoOpenModeDescriptionBrowser");
                    case Settings.Reading.WallabagVideoOpenMode.App:
                        return _device.GetLocalizedResource("VideoOpenModeDescriptionApp");
                    default:
                    case Settings.Reading.WallabagVideoOpenMode.Inline:
                        return _device.GetLocalizedResource("VideoOpenModeDescriptionInline");
                }
            }
        }

        public bool BackgroundTaskIsEnabled
        {
            get { return Settings.BackgroundTask.IsEnabled; }
            set
            {
                Settings.BackgroundTask.IsEnabled = value;
                _backgroundTaskOptionsChanged = true;
                RaisePropertyChanged();
            }
        }
        public bool BackgroundTaskIsNotSupported => _backgroundTaskService.IsSupported == false;
        public bool BackgroundTaskIsSupported => _backgroundTaskService.IsSupported;
        public double BackgroundTaskExecutionInterval
        {
            get { return Settings.BackgroundTask.ExecutionInterval.TotalMinutes; }
            set
            {
                Settings.BackgroundTask.ExecutionInterval = TimeSpan.FromMinutes(value);
                _backgroundTaskOptionsChanged = true;
                RaisePropertyChanged();
            }
        }
        public bool DownloadNewItemsDuringExecutionOfBackgroundTask
        {
            get { return Settings.BackgroundTask.DownloadNewItemsDuringExecution; }
            set
            {
                Settings.BackgroundTask.DownloadNewItemsDuringExecution = value;
                RaisePropertyChanged();
            }
        }

        public bool VideoOpenModeIsInline => Settings.Reading.VideoOpenMode == Settings.Reading.WallabagVideoOpenMode.Inline;
        public bool VideoOpenModeIsApp => Settings.Reading.VideoOpenMode == Settings.Reading.WallabagVideoOpenMode.App;
        public bool VideoOpenModeIsBrowser => Settings.Reading.VideoOpenMode == Settings.Reading.WallabagVideoOpenMode.Browser;

        public void SetVideoOpenMode(Settings.Reading.WallabagVideoOpenMode mode)
        {
            Settings.Reading.VideoOpenMode = mode;

            RaisePropertyChanged(nameof(VideoOpenModeIsApp));
            RaisePropertyChanged(nameof(VideoOpenModeIsInline));
            RaisePropertyChanged(nameof(VideoOpenModeIsBrowser));
            RaisePropertyChanged(nameof(VideoOpenModeDescription));
        }

        [DependsOn(nameof(BackgroundTaskExecutionInterval))]
        public string BackgroundTaskExecutionIntervalDescription => string.Format(_device.GetLocalizedResource("BackgroundTaskExecutionIntervalInMinutesTextBlock.Text"), BackgroundTaskExecutionInterval);
        public string BackgroundTaskLastExecutionDescription
        => string.Format(_device.GetLocalizedResource("LastExecutionOfBackgroundTaskTextBlock.Text"),
            Settings.BackgroundTask.LastExecution == DateTime.MinValue
            ? _device.GetLocalizedResource("Never")
            : Settings.BackgroundTask.LastExecution.ToString());

        private bool _backgroundTaskOptionsChanged = false;

        public ICommand OpenDocumentationCommand { get; private set; }
        public ICommand OpenWallabagTwitterAccountCommand { get; set; }
        public ICommand ContactDeveloperCommand { get; private set; }
        public ICommand CreateIssueCommand { get; private set; }
        public ICommand RateAppCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
        public ICommand DeleteDatabaseCommand { get; private set; }

        public SettingsPageViewModel(
            ILoggingService logging,
            IBackgroundTaskService backgroundTask,
            IPlatformSpecific device,
            SQLiteConnection database)
        {
            _loggingService = logging;
            _backgroundTaskService = backgroundTask;
            _device = device;
            _database = database;

            _loggingService.WriteLine($"Creating a new instance of {nameof(SettingsPageViewModel)}.");

            OpenDocumentationCommand = new RelayCommand(() => _device.LaunchUri(_documentationUri));
            OpenWallabagTwitterAccountCommand = new RelayCommand(() => _device.LaunchUri(_twitterAccountUri));
            ContactDeveloperCommand = new RelayCommand(() => _device.LaunchUri(_mailUri));
            CreateIssueCommand = new RelayCommand(() => _device.LaunchUri(_githubIssueUri));
            RateAppCommand = new RelayCommand(() => _device.LaunchUri(_rateAppUri));
            LogoutCommand = new RelayCommand(() => Logout());
            DeleteDatabaseCommand = new RelayCommand(() => DeleteDatabase());
        }

        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState)
        {
            if (_backgroundTaskOptionsChanged)
            {
                if (BackgroundTaskIsEnabled)
                {
                    _loggingService.WriteLine("The background task options were changed. Re-registering the background task.");
                    _backgroundTaskService.UnregisterBackgroundTask();
                    return _backgroundTaskService.RegisterBackgroundTaskAsync();
                }
                else
                {
                    _loggingService.WriteLine("The background task options were changed. Unregistering the background task.");
                    _backgroundTaskService.UnregisterBackgroundTask();
                }
            }
            return Task.FromResult(true);
        }

        private void Logout()
        {
            _loggingService.WriteLine("Deleting all credentials.");
            Settings.Authentication.AccessToken = string.Empty;
            Settings.Authentication.RefreshToken = string.Empty;
            Settings.Authentication.LastTokenRefreshDateTime = DateTime.MinValue;

            DeleteDatabase();
        }
        private void DeleteDatabase()
        {
            _loggingService.WriteLine("Deleting the database.");
            string path = _database.DatabasePath;
            _database.Close();

            _device.DeleteDatabaseAsync();
            _device.CloseApplication();
        }
    }
}

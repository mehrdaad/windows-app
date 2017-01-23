using GalaSoft.MvvmLight.Command;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using wallabag.Data.Common;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;

namespace wallabag.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class SettingsPageViewModel : ViewModelBase
    {
        private Uri _documentationUri = new Uri("http://doc.wallabag.org/");
        private Uri _twitterAccountUri = new Uri("https://twitter.com/wallabagapp");
        private Uri _mailUri = new Uri("mailto:jlnostr+wallabag@outlook.de");
        private Uri _githubIssueUri = new Uri("https://github.com/wallabag/windows-app/issues/new");
        private Uri _rateAppUri = new Uri("ms-windows-store://review/?ProductId=" + Package.Current.Id.FamilyName);
        private IBackgroundTaskService _backgroundTaskService;

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
        public string VersionNumber => GetVersionNumber();
        public string VideoOpenModeDescription
        {
            get
            {
                switch (Settings.Reading.VideoOpenMode)
                {
                    case Settings.Reading.WallabagVideoOpenMode.Browser:
                        return GeneralHelper.LocalizedResource("VideoOpenModeDescriptionBrowser");
                    case Settings.Reading.WallabagVideoOpenMode.App:
                        return GeneralHelper.LocalizedResource("VideoOpenModeDescriptionApp");
                    default:
                    case Settings.Reading.WallabagVideoOpenMode.Inline:
                        return GeneralHelper.LocalizedResource("VideoOpenModeDescriptionInline");
                }
            }
        }

        public bool WhiteOverlayForTitleBar
        {
            get { return Settings.SettingsService.GetValueOrDefault(nameof(WhiteOverlayForTitleBar), true); }
            set
            {
                Settings.SettingsService.AddOrUpdateValue(nameof(WhiteOverlayForTitleBar), value);
                RaisePropertyChanged();
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

        public bool? VideoOpenModeIsInline => Settings.Reading.VideoOpenMode == Settings.Reading.WallabagVideoOpenMode.Inline;
        public bool? VideoOpenModeIsApp => Settings.Reading.VideoOpenMode == Settings.Reading.WallabagVideoOpenMode.App;
        public bool? VideoOpenModeIsBrowser => Settings.Reading.VideoOpenMode == Settings.Reading.WallabagVideoOpenMode.Browser;

        [DependsOn(nameof(BackgroundTaskExecutionInterval))]
        public string BackgroundTaskExecutionIntervalDescription => string.Format(GeneralHelper.LocalizedResource("BackgroundTaskExecutionIntervalInMinutesTextBlock.Text"), BackgroundTaskExecutionInterval);
        public string BackgroundTaskLastExecutionDescription
        => string.Format(GeneralHelper.LocalizedResource("LastExecutionOfBackgroundTaskTextBlock.Text"),
            Settings.BackgroundTask.LastExecution == DateTime.MinValue
            ? GeneralHelper.LocalizedResource("Never")
            : Settings.BackgroundTask.LastExecution.ToString());

        private bool _backgroundTaskOptionsChanged = false;

        public ICommand OpenDocumentationCommand { get; private set; }
        public ICommand OpenWallabagTwitterAccountCommand { get; set; }
        public ICommand ContactDeveloperCommand { get; private set; }
        public ICommand CreateIssueCommand { get; private set; }
        public ICommand RateAppCommand { get; private set; }
        public ICommand TellFriendsCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
        public ICommand DeleteDatabaseCommand { get; private set; }
        public ICommand VideoOpenModeRadioButtonCheckedCommand { get; private set; }

        public SettingsPageViewModel(IBackgroundTaskService backgroundTaskService)
        {
            _backgroundTaskService = backgroundTaskService;

            OpenDocumentationCommand = new RelayCommand(async () => await Launcher.LaunchUriAsync(_documentationUri));
            OpenWallabagTwitterAccountCommand = new RelayCommand(async () => await Launcher.LaunchUriAsync(_twitterAccountUri));
            ContactDeveloperCommand = new RelayCommand(async () => await Launcher.LaunchUriAsync(_mailUri));
            CreateIssueCommand = new RelayCommand(async () => await Launcher.LaunchUriAsync(_githubIssueUri));
            RateAppCommand = new RelayCommand(async () => await Launcher.LaunchUriAsync(_rateAppUri));
            TellFriendsCommand = new RelayCommand(() => TellFriends());
            LogoutCommand = new RelayCommand(() => Logout());
            DeleteDatabaseCommand = new RelayCommand(() => DeleteDatabase());
            VideoOpenModeRadioButtonCheckedCommand = new RelayCommand<string>(mode => VideoOpenModeRadioButtonChecked(mode));
        }

        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState)
        {
            if (_backgroundTaskOptionsChanged && BackgroundTaskIsEnabled)
            {
                _backgroundTaskService.UnregisterBackgroundTask();
                return _backgroundTaskService.RegisterBackgroundTaskAsync();
            }
            return Task.CompletedTask;
        }

        private string GetVersionNumber()
        {
            var version = Package.Current.Id.Version;
            return string.Format($"{version.Major}.{version.Minor}.{version.Build}");
        }
        private void TellFriends()
        {
            DataTransferManager.GetForCurrentView().DataRequested += (s, e) =>
            {
                e.Request.Data.SetWebLink(new Uri("https://www.wallabag.org/"));
                e.Request.Data.Properties.ApplicationName = Package.Current.DisplayName;
                e.Request.Data.Properties.Title = GeneralHelper.LocalizedResource("TellFriendsQuestion");
            };
            DataTransferManager.ShowShareUI();
        }
        private void Logout()
        {
            Settings.Authentication.AccessToken = string.Empty;
            Settings.Authentication.RefreshToken = string.Empty;
            Settings.Authentication.LastTokenRefreshDateTime = DateTime.MinValue;

            DeleteDatabase();
        }
        private void DeleteDatabase()
        {
            string path = Database.DatabasePath;
            Database.Close();

            File.Delete(path);

            Application.Current.Exit();
        }

        private void VideoOpenModeRadioButtonChecked(string mode)
        {
            switch (mode)
            {
                case "app":
                    Settings.Reading.VideoOpenMode = Settings.Reading.WallabagVideoOpenMode.App;
                    break;
                case "browser":
                    Settings.Reading.VideoOpenMode = Settings.Reading.WallabagVideoOpenMode.Browser;
                    break;
                case "inline":
                default:
                    Settings.Reading.VideoOpenMode = Settings.Reading.WallabagVideoOpenMode.Inline;
                    break;
            }

            RaisePropertyChanged(nameof(VideoOpenModeDescription));
            RaisePropertyChanged(nameof(VideoOpenModeIsApp));
            RaisePropertyChanged(nameof(VideoOpenModeIsBrowser));
            RaisePropertyChanged(nameof(VideoOpenModeIsInline));
        }
    }
}

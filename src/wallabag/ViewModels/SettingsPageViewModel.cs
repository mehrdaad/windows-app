using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Common.Helpers;
using wallabag.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class SettingsPageViewModel : ViewModelBase
    {
        private Uri _changelogUri = new Uri("https://github.com/wallabag/windows-app/blob/master/README.md");
        private Uri _documentationUri = new Uri("http://doc.wallabag.org/");
        private Uri _twitterAccountUri = new Uri("https://twitter.com/wallabagapp");
        private Uri _mailUri = new Uri("mailto:jlnostr@outlook.de");
        private Uri _githubIssueUri = new Uri("https://github.com/wallabag/windows-app/issues/new");
        private Uri _rateAppUri = new Uri("ms-windows-store://review/?ProductId=" + Package.Current.Id.FamilyName);

        public bool SyncOnStartup { get; set; } = SettingsService.Instance.SyncOnStartup;
        public bool AllowCollectionOfTelemetryData { get; set; } = SettingsService.Instance.AllowCollectionOfTelemetryData;
        public bool NavigateBackAfterReadingAnArticle { get; set; } = SettingsService.Instance.NavigateBackAfterReadingAnArticle;
        public bool SyncReadingProgress { get; set; } = SettingsService.Instance.SyncReadingProgress;
        public string VersionNumber { get; set; }
        public string VideoOpenModeDescription
        {
            get
            {
                switch (SettingsService.Instance.VideoOpenMode)
                {
                    case SettingsService.WallabagVideoOpenMode.Browser:
                        return GeneralHelper.LocalizedResource("VideoOpenModeDescriptionBrowser");
                    case SettingsService.WallabagVideoOpenMode.App:
                        return GeneralHelper.LocalizedResource("VideoOpenModeDescriptionApp");
                    default:
                    case SettingsService.WallabagVideoOpenMode.Inline:
                        return GeneralHelper.LocalizedResource("VideoOpenModeDescriptionInline");
                }
            }
        }
        public bool WhiteOverlayForTitleBar { get; set; } = SettingsService.Instance.WhiteOverlayForTitleBar;
        public bool BackgroundTaskIsEnabled { get; set; } = SettingsService.Instance.BackgroundTaskIsEnabled && BackgroundTaskHelper.IsSupportedOnDevice;
        public bool BackgroundTaskIsNotSupported { get; } = BackgroundTaskHelper.IsSupportedOnDevice == false;
        public bool BackgroundTaskIsSupported { get; } = BackgroundTaskHelper.IsSupportedOnDevice;
        public double BackgroundTaskExecutionInterval { get; set; } = SettingsService.Instance.BackgroundTaskExecutionInterval;
        public bool DownloadNewItemsDuringExecutionOfBackgroundTask { get; set; } = SettingsService.Instance.DownloadNewItemsDuringExecutionOfBackgroundTask;

        public bool? VideoOpenModeIsInline { get { return SettingsService.Instance.VideoOpenMode == SettingsService.WallabagVideoOpenMode.Inline; } }
        public bool? VideoOpenModeIsApp { get { return SettingsService.Instance.VideoOpenMode == SettingsService.WallabagVideoOpenMode.App; } }
        public bool? VideoOpenModeIsBrowser { get { return SettingsService.Instance.VideoOpenMode == SettingsService.WallabagVideoOpenMode.Browser; } }

        [DependsOn(nameof(BackgroundTaskExecutionInterval))]
        public string BackgroundTaskExecutionIntervalDescription { get { return string.Format(GeneralHelper.LocalizedResource("BackgroundTaskExecutionIntervalInMinutesTextBlock.Text"), BackgroundTaskExecutionInterval); } }
        public string BackgroundTaskLastExecutionDescription
        {
            get
            {
                return string.Format(GeneralHelper.LocalizedResource("LastExecutionOfBackgroundTaskTextBlock.Text"),
                    SettingsService.Instance.LastExecutionOfBackgroundTask == DateTime.MinValue ? GeneralHelper.LocalizedResource("Never") : SettingsService.Instance.LastExecutionOfBackgroundTask.ToString());
            }
        }
        private bool _backgroundTaskOptionsChanged = false;

        public DelegateCommand OpenChangelogCommand { get; private set; }
        public DelegateCommand OpenDocumentationCommand { get; private set; }
        public DelegateCommand OpenWallabagTwitterAccountCommand { get; set; }
        public DelegateCommand ContactDeveloperCommand { get; private set; }
        public DelegateCommand CreateIssueCommand { get; private set; }
        public DelegateCommand RateAppCommand { get; private set; }
        public DelegateCommand TellFriendsCommand { get; private set; }
        public DelegateCommand LogoutCommand { get; private set; }
        public DelegateCommand DeleteDatabaseCommand { get; private set; }
        public DelegateCommand<string> VideoOpenModeRadioButtonCheckedCommand { get; private set; }

        public SettingsPageViewModel()
        {
            VersionNumber = GetVersionNumber();

            OpenChangelogCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(_changelogUri));
            OpenDocumentationCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(_documentationUri));
            OpenWallabagTwitterAccountCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(_twitterAccountUri));
            ContactDeveloperCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(_mailUri));
            CreateIssueCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(_githubIssueUri));
            RateAppCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(_rateAppUri));
            TellFriendsCommand = new DelegateCommand(() => TellFriends());
            LogoutCommand = new DelegateCommand(() => Logout());
            DeleteDatabaseCommand = new DelegateCommand(() => DeleteDatabase());
            VideoOpenModeRadioButtonCheckedCommand = new DelegateCommand<string>(mode => VideoOpenModeRadioButtonChecked(mode));

            PropertyChanged += (s, e) =>
            {
                var settings = SettingsService.Instance;
                switch (e.PropertyName)
                {
                    case nameof(SyncOnStartup):
                        settings.SyncOnStartup = SyncOnStartup; break;
                    case nameof(AllowCollectionOfTelemetryData):
                        settings.AllowCollectionOfTelemetryData = AllowCollectionOfTelemetryData; break;
                    case nameof(NavigateBackAfterReadingAnArticle):
                        settings.NavigateBackAfterReadingAnArticle = NavigateBackAfterReadingAnArticle; break;
                    case nameof(SyncReadingProgress):
                        settings.SyncReadingProgress = SyncReadingProgress; break;
                    case nameof(WhiteOverlayForTitleBar):
                        settings.WhiteOverlayForTitleBar = WhiteOverlayForTitleBar; break;
                    case nameof(DownloadNewItemsDuringExecutionOfBackgroundTask):
                        settings.DownloadNewItemsDuringExecutionOfBackgroundTask = DownloadNewItemsDuringExecutionOfBackgroundTask; break;
                    case nameof(BackgroundTaskExecutionInterval):
                    case nameof(BackgroundTaskIsEnabled):
                        if (BackgroundTaskIsEnabled == false)
                        {
                            SettingsService.Instance.LastExecutionOfBackgroundTask = DateTime.MinValue;
                            RaisePropertyChanged(nameof(BackgroundTaskLastExecutionDescription));
                        }

                        _backgroundTaskOptionsChanged = true;
                        break;
                }
            };
        }

        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            if (_backgroundTaskOptionsChanged == false)
                return Task.CompletedTask;

            SettingsService.Instance.BackgroundTaskIsEnabled = BackgroundTaskIsEnabled;
            SettingsService.Instance.BackgroundTaskExecutionInterval = (int)BackgroundTaskExecutionInterval;

            if (BackgroundTaskIsEnabled)
            {
                BackgroundTaskHelper.UnregisterBackgroundTask();
                return BackgroundTaskHelper.RegisterBackgroundTaskAsync();
            }
            return Task.CompletedTask;
        }

        private string GetVersionNumber()
        {
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

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
            SettingsService.Instance.AccessToken = string.Empty;
            SettingsService.Instance.RefreshToken = string.Empty;
            SettingsService.Instance.LastTokenRefreshDateTime = DateTime.MinValue;

            DeleteDatabase();
        }
        private void DeleteDatabase()
        {
            string path = App.Database.DatabasePath;
            App.Database.Close();

            File.Delete(path);

            Application.Current.Exit();
        }

        private void VideoOpenModeRadioButtonChecked(string mode)
        {
            switch (mode)
            {
                case "app":
                    SettingsService.Instance.VideoOpenMode = SettingsService.WallabagVideoOpenMode.App;
                    break;
                case "browser":
                    SettingsService.Instance.VideoOpenMode = SettingsService.WallabagVideoOpenMode.Browser;
                    break;
                case "inline":
                default:
                    SettingsService.Instance.VideoOpenMode = SettingsService.WallabagVideoOpenMode.Inline;
                    break;
            }

            RaisePropertyChanged(nameof(VideoOpenModeDescription));
            RaisePropertyChanged(nameof(VideoOpenModeIsApp));
            RaisePropertyChanged(nameof(VideoOpenModeIsBrowser));
            RaisePropertyChanged(nameof(VideoOpenModeIsInline));
        }
    }
}

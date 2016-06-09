using PropertyChanged;
using System;
using System.IO;
using Template10.Mvvm;
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
        // TODO: Change after merged into master branch.
        private Uri _changelogUri = new Uri("https://github.com/wallabag/windows-app/blob/v2/CHANGELOG.md");
        private Uri _documentationUri = new Uri("http://doc.wallabag.org/");
        private Uri _twitterAccountUri = new Uri("http://doc.wallabag.org/");
        private Uri _mailUri = new Uri("mailto:wallabag@jlnostr.de");
        private Uri _githubIssueUri = new Uri("https://github.com/wallabag/windows-app/issues/new");
        private Uri _rateAppUri = new Uri("ms-windows-store://review/?ProductId=" + Package.Current.Id.FamilyName);

        public bool SyncOnStartup { get; set; } = SettingsService.Instance.SyncOnStartup;
        public bool AllowCollectionOfTelemetryData { get; set; } = SettingsService.Instance.AllowCollectionOfTelemetryData;
        public bool NavigateBackAfterReadingAnArticle { get; set; } = SettingsService.Instance.NavigateBackAfterReadingAnArticle;
        public bool SyncReadingProgress { get; set; } = SettingsService.Instance.SyncReadingProgress;
        public string VersionNumber { get; set; }

        public DelegateCommand OpenChangelogCommand { get; private set; }
        public DelegateCommand OpenDocumentationCommand { get; private set; }
        public DelegateCommand OpenWallabagTwitterAccountCommand { get; set; }
        public DelegateCommand ContactDeveloperCommand { get; private set; }
        public DelegateCommand CreateIssueCommand { get; private set; }
        public DelegateCommand RateAppCommand { get; private set; }
        public DelegateCommand TellFriendsCommand { get; private set; }
        public DelegateCommand LogoutCommand { get; private set; }
        public DelegateCommand DeleteDatabaseCommand { get; private set; }

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
                }
            };
        }

        private string GetVersionNumber()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format($"{version.Major}.{version.Minor}.{version.Build}");
        }
        private void TellFriends()
        {
            // TODO: Update strings.
            DataTransferManager.GetForCurrentView().DataRequested += (s, e) =>
            {
                e.Request.Data.SetText("YO, CHECK OUT WALLABAG!");
                e.Request.Data.SetWebLink(_changelogUri);
                e.Request.Data.Properties.ApplicationName = Package.Current.DisplayName;
                e.Request.Data.Properties.Title = "Have you heard of wallabag?";
            };
            DataTransferManager.ShowShareUI();
        }
        private void Logout()
        {
            SettingsService.Instance.AccessToken = string.Empty;
            SettingsService.Instance.RefreshToken = string.Empty;
            SettingsService.Instance.LastTokenRefreshDateTime = DateTime.MinValue;

            Application.Current.Exit();
        }
        private void DeleteDatabase()
        {
            var path = App.Database.DatabasePath;
            App.Database.Close();

            File.Delete(path);

            Application.Current.Exit();
        }
    }
}

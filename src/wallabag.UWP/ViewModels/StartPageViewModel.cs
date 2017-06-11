using GalaSoft.MvvmLight.Ioc;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Api.Models;
using wallabag.Data.Common;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using wallabag.Data.Services.MigrationService;
using wallabag.Data.ViewModels;
using wallabag.Models;

namespace wallabag.ViewModels
{
    public class StartPageViewModel : ViewModelBase
    {
        private IMigrationService _migrationService;
        private SQLiteConnection _database;
        private INavigationService _navigationService;
        private IWallabagClient _client;
        private IPlatformSpecific _device;
        private ILoggingService _logging;

        public bool IsActive { get; set; }
        public string ProgressDescription { get; set; }

        public StartPageViewModel()
        {
            _migrationService = SimpleIoc.Default.GetInstance<IMigrationService>();
            _database = SimpleIoc.Default.GetInstance<SQLiteConnection>();
            _navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            _client = SimpleIoc.Default.GetInstance<IWallabagClient>();
            _device = SimpleIoc.Default.GetInstance<IPlatformSpecific>();
            _logging = SimpleIoc.Default.GetInstance<ILoggingService>();
        }

        public override async Task ActivateAsync(object parameter, IDictionary<string, object> state, NavigationMode mode)
        {
            var param = parameter as StartPageNavigationParameter;

            if (_migrationService.Check(param.PreviousVersion))
                ExecuteMigrations(param.PreviousVersion);

            Settings.General.AppVersion = _device.AppVersion;

            if (string.IsNullOrEmpty(Settings.Authentication.AccessToken) ||
                string.IsNullOrEmpty(Settings.Authentication.RefreshToken))
            {
                _logging.WriteLine($"Credentials not found. Navigating to the {nameof(Navigation.Pages.LoginPage)}.");
                _navigationService.Navigate(Navigation.Pages.LoginPage);
                _navigationService.ClearHistory();
            }
            else
            {
                if (!param.DatabaseExists)
                {
                    _logging.WriteLine("Database does not exist. Downloading all entries and tags.");
                    await DownloadAllAsync();
                }

                _navigationService.Navigate(Navigation.Pages.MainPage);
                _navigationService.ClearHistory();
            }
        }

        private void ExecuteMigrations(Version oldVersion)
        {
            IsActive = true;
            ProgressDescription = _device.GetLocalizedResource("MigratingProgress");

            _migrationService.ExecuteAll(oldVersion);

            IsActive = false;
        }
        private async Task DownloadAllAsync()
        {
            IsActive = true;

            ProgressDescription = _device.GetLocalizedResource("DownloadingItemsTextBlock.Text");

            var itemResponse = await _client.GetItemsWithEnhancedMetadataAsync(itemsPerPage: 100);
            var items = itemResponse.Items as List<WallabagItem>;

            if (itemResponse.Pages >= 2)
                for (int i = 2; i < itemResponse.Pages; i++)
                {
                    _logging.WriteLine($"Downloading items for page {i}.");
                    ProgressDescription = string.Format(_device.GetLocalizedResource("DownloadingItemsWithProgress"), items.Count, itemResponse.TotalNumberOfItems);
                    items.AddRange(await _client.GetItemsAsync(itemsPerPage: 100, pageNumber: i));
                }

            var finalItemList = new List<Item>();
            foreach (var item in items)
                finalItemList.Add(item);

            _logging.WriteLine("Fetching tags from server.");
            var tags = await _client.GetTagsAsync();
            var tagList = new List<Tag>();

            foreach (var tag in tags)
                tagList.Add(tag);

            _logging.WriteLine("Saving items and tags in the database.");
            _database.InsertOrReplaceAll(finalItemList);
            _database.InsertOrReplaceAll(tagList);

            IsActive = false;
        }
    }
}
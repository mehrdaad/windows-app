using GalaSoft.MvvmLight.Command;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Models;
using wallabag.Data.Services;

namespace wallabag.Data.ViewModels
{
    public class AddItemViewModel : ViewModelBase
    {
        private readonly IOfflineTaskService _offlineTaskService;
        private readonly ILoggingService _loggingService;
        private readonly SQLiteConnection _database;
        private readonly INavigationService _navigationService;

        public string UriString { get; set; } = string.Empty;
        public IEnumerable<Tag> Tags { get; set; } = new ObservableCollection<Tag>();

        public ICommand AddCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public AddItemViewModel(IOfflineTaskService offlineTaskService, ILoggingService loggingService, SQLiteConnection database, INavigationService navigationService) 
        {
            _offlineTaskService = offlineTaskService;
            _loggingService = loggingService;
            _database = database;
            _navigationService = navigationService;

            AddCommand = new RelayCommand(() => Add());
            CancelCommand = new RelayCommand(() => Cancel());
        }

        private void Add()
        {
            _loggingService.WriteLine("Adding item to wallabag.");
            _loggingService.WriteLine($"URL: {UriString}");
            _loggingService.WriteLine($"Tags: {string.Join(",", Tags)}");

            if (UriString.IsValidUri())
            {
                _loggingService.WriteLine("URL is valid.");
                var uri = new Uri(UriString);

                _loggingService.WriteLine("Inserting new placeholder item into the database.");
                _database.Insert(new Item()
                {
                    Id = _offlineTaskService.LastItemId + 1,
                    Title = uri.Host,
                    Url = UriString,
                    Hostname = uri.Host
                });

                _offlineTaskService.Add(UriString, Tags.ToStringArray());
                _navigationService.GoBack();
            }
        }

        private void Cancel()
        {
            _loggingService.WriteLine("Cancelling the addition of another item.");
            _navigationService.GoBack();
        }
    }
}

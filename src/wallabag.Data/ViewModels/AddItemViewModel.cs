using GalaSoft.MvvmLight.Command;
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
        public string UriString { get; set; } = string.Empty;
        public IEnumerable<Tag> Tags { get; set; } = new ObservableCollection<Tag>();

        public ICommand AddCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public AddItemViewModel()
        {
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
                    Id = OfflineTaskService.LastItemId + 1,
                    Title = uri.Host,
                    Url = UriString,
                    Hostname = uri.Host
                });

                OfflineTaskService.Add(UriString, Tags.ToStringArray());
            }
        }

        private void Cancel()
        {
            _loggingService.WriteLine("Cancelling the addition of another item.");
            _navigationService.GoBack();
        }
    }
}

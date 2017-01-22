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
            LoggingService.WriteLine("Adding item to wallabag.");
            LoggingService.WriteLine($"URL: {UriString}");
            LoggingService.WriteLine($"Tags: {string.Join(",", Tags)}");

            if (UriString.IsValidUri())
            {
                LoggingService.WriteLine("URL is valid.");
                var uri = new Uri(UriString);

                LoggingService.WriteLine("Inserting new placeholder item into the database.");
                Database.Insert(new Item()
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
            LoggingService.WriteLine("Cancelling the addition of another item. Closing dialog now.");
            DialogService.HideCurrentDialog();
        }
    }
}

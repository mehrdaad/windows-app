using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using SQLite.Net;
using System;
using System.Windows.Input;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Models;
using wallabag.Data.Services;
using wallabag.Data.Services.OfflineTaskService;

namespace wallabag.Data.ViewModels
{
    public class AddItemViewModel : ViewModelBase
    {
        private readonly IOfflineTaskService _offlineTaskService;
        private readonly ILoggingService _loggingService;
        private readonly SQLiteConnection _database;
        private readonly INavigationService _navigationService;

        public string UriString { get; set; } = string.Empty;
        public EditTagsViewModel TagViewModel { get; private set; }

        public ICommand AddCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public event EventHandler<EventArgs> OnAddingCompleted;

        public AddItemViewModel(IOfflineTaskService offlineTaskService, ILoggingService loggingService, SQLiteConnection database, INavigationService navigationService)
        {
            _offlineTaskService = offlineTaskService;
            _loggingService = loggingService;
            _database = database;
            _navigationService = navigationService;

            TagViewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService);

            AddCommand = new RelayCommand(() => Add());
            CancelCommand = new RelayCommand(() => Cancel());
        }

        private void Add()
        {
            _loggingService.WriteLine("Adding item to wallabag.");
            _loggingService.WriteLine($"URL: {UriString}");
            _loggingService.WriteLine($"Tags: {string.Join(",", TagViewModel.Tags)}");

            if (UriString.IsValidUri() &&
                Uri.TryCreate(UriString, UriKind.Absolute, out var uri))
            {
                _loggingService.WriteLine("URL is valid.");

                _offlineTaskService.AddAsync(UriString, TagViewModel.Tags.ToStringArray());
                _navigationService.GoBack();

                OnAddingCompleted?.Invoke(this, null);
            }
        }

        private void Cancel()
        {
            _loggingService.WriteLine("Cancelling the addition of another item.");
            _navigationService.GoBack();
        }
    }
}

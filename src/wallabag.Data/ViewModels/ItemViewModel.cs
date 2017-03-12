using GalaSoft.MvvmLight.Command;
using SQLite.Net;
using System;
using System.ComponentModel;
using System.Windows.Input;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using static wallabag.Data.Common.Navigation;

namespace wallabag.Data.ViewModels
{
    public class ItemViewModel : ViewModelBase, IComparable
    {
        private readonly IOfflineTaskService _offlineTaskService;
        private readonly INavigationService _navigationService;
        private readonly ILoggingService _loggingService;
        private readonly IPlatformSpecific _device;
        private readonly SQLiteConnection _database;

        public Item Model { get; private set; }

        public string TagsString => string.Join(", ", Model.Tags);
        public bool TagsAreExisting => Model.Tags.Count > 0;

        public ICommand MarkAsReadCommand { get; private set; }
        public ICommand UnmarkAsReadCommand { get; private set; }
        public ICommand MarkAsStarredCommand { get; private set; }
        public ICommand UnmarkAsStarredCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand ShareCommand { get; private set; }
        public ICommand EditTagsCommand { get; private set; }
        public ICommand OpenInBrowserCommand { get; private set; }

        public ItemViewModel(Item item,
            IOfflineTaskService offlineTaskService,
            INavigationService navigation,
            ILoggingService logging,
            IPlatformSpecific device,
            SQLiteConnection database)
        {
            Model = item;
            _offlineTaskService = offlineTaskService;
            _navigationService = navigation;
            _loggingService = logging;
            _device = device;
            _database = database;

            (item as INotifyPropertyChanged).PropertyChanged += (s, e) =>
            {
                _loggingService.WriteLine($"Model with ID {item.Id} was updated.");
                RaisePropertyChanged(nameof(item));
            };

            MarkAsReadCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Marking item {item.Id} as read.");
                item.IsRead = true;
                UpdateItem();
                _offlineTaskService.Add(item.Id, OfflineTask.OfflineTaskAction.MarkAsRead);
            });
            UnmarkAsReadCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Marking item {item.Id} as unread.");
                item.IsRead = false;
                UpdateItem();
                _offlineTaskService.Add(item.Id, OfflineTask.OfflineTaskAction.UnmarkAsRead);
            });
            MarkAsStarredCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Marking item {item.Id} as favorite.");
                item.IsStarred = true;
                UpdateItem();
                _offlineTaskService.Add(item.Id, OfflineTask.OfflineTaskAction.MarkAsStarred);
            });
            UnmarkAsStarredCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Marking item {item.Id} as unfavorite.");
                item.IsStarred = false;
                UpdateItem();
                _offlineTaskService.Add(item.Id, OfflineTask.OfflineTaskAction.UnmarkAsStarred);
            });
            DeleteCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Deleting item {item.Id}.");
                _database.Delete(item);
                _offlineTaskService.Add(item.Id, OfflineTask.OfflineTaskAction.Delete);
            });
            ShareCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Sharing item {item.Id}.");
                _device.ShareItem(item);
            });
            EditTagsCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Editing tags of item {item.Id}.");
                _navigationService.Navigate(Pages.EditTagsPage, Model.Id);
            });
            OpenInBrowserCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Opening item {item.Id} in browser.");
                _device.LaunchUri(new Uri(item.Url));
            });
        }

        public static ItemViewModel FromId(
            int itemId,
            ILoggingService logging,
            SQLiteConnection database,
            IOfflineTaskService offlineTaskService,
            INavigationService navigation,
            IPlatformSpecific platform)
        {
            logging.WriteLine($"Creating ItemViewModel from item id: {itemId}");

            var item = database.Find<Item>(itemId);

            if (item == null)
                logging.WriteLine($"Failed! Item does not exist in database!", LoggingCategory.Critical);

            if (item != null)
                return new ItemViewModel(item, offlineTaskService, navigation, logging, platform, database);
            else
                return null;
        }

        private void UpdateItem()
        {
            _loggingService.WriteLine($"Updating item {Model.Id} in database.");
            Model.LastModificationDate = DateTime.UtcNow;
            _database.Update(Model);
        }

        public int CompareTo(object obj) => ((IComparable)Model).CompareTo((obj as ItemViewModel).Model);
        public override bool Equals(object obj) => Model.Equals((obj as ItemViewModel).Model);
        public override int GetHashCode() => Model.GetHashCode();
    }
}
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.ComponentModel;
using System.Windows.Input;
using wallabag.Data.Models;
using wallabag.Data.Services;
using static wallabag.Data.Common.Navigation;

namespace wallabag.Data.ViewModels
{
    public class ItemViewModel : ViewModelBase, IComparable
    {
        private IOfflineTaskService _offlineTaskService => SimpleIoc.Default.GetInstance<IOfflineTaskService>();

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

        public ItemViewModel(Item Model)
        {
            _loggingService.WriteLine("Creating new instance of ItemViewModel.");
            _loggingService.WriteLine($"{Model.Id} | {Model.Title} | {Model.Url}");

            this.Model = Model;

            (Model as INotifyPropertyChanged).PropertyChanged += (s, e) =>
            {
                _loggingService.WriteLine($"Model with ID {Model.Id} was updated.");
                RaisePropertyChanged(nameof(Model));
            };
            Model.Tags.CollectionChanged += (s, e) =>
            {
                _loggingService.WriteLine($"Tags of model with ID {Model.Id} were updated.");
                RaisePropertyChanged(nameof(TagsString));
                RaisePropertyChanged(nameof(TagsAreExisting));
            };

            MarkAsReadCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Marking item {Model.Id} as read.");
                Model.IsRead = true;
                UpdateItem();
                _offlineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.MarkAsRead);
            });
            UnmarkAsReadCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Marking item {Model.Id} as unread.");
                Model.IsRead = false;
                UpdateItem();
                _offlineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.UnmarkAsRead);
            });
            MarkAsStarredCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Marking item {Model.Id} as favorite.");
                Model.IsStarred = true;
                UpdateItem();
                _offlineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.MarkAsStarred);
            });
            UnmarkAsStarredCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Marking item {Model.Id} as unfavorite.");
                Model.IsStarred = false;
                UpdateItem();
                _offlineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.UnmarkAsStarred);
            });
            DeleteCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Deleting item {Model.Id}.");
                _database.Delete(Model);
                _offlineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.Delete);
            });
            ShareCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Sharing item {Model.Id}.");
                Device.ShareItem(Model);
            });
            EditTagsCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Editing tags of item {Model.Id}.");
                _navigationService.Navigate(Pages.EditTagsPage, new EditTagsViewModel(this.Model));
            });
            OpenInBrowserCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Opening item {Model.Id} in browser.");
                Device.LaunchUri(new Uri(Model.Url));
            });
        }

        public static ItemViewModel FromId(int itemId)
        {
            var ls = SimpleIoc.Default.GetInstance<ILoggingService>();
            ls.WriteLine($"Creating ItemViewModel from item id: {itemId}");

            var item = _database.Find<Item>(itemId);
            ls.WriteLineIf(item == null, $"Failed! Item does not exist in database!", LoggingCategory.Critical);

            if (item != null)
                return new ItemViewModel(item);
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
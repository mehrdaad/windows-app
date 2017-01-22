using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.ComponentModel;
using System.Windows.Input;
using wallabag.Data.Common;
using wallabag.Data.Models;
using wallabag.Data.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace wallabag.Data.ViewModels
{
    public class ItemViewModel : ViewModelBase, IComparable
    {
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
            LoggingService.WriteLine("Creating new instance of ItemViewModel.");
            LoggingService.WriteLine($"{Model.Id} | {Model.Title} | {Model.Url}");

            this.Model = Model;

            (Model as INotifyPropertyChanged).PropertyChanged += (s, e) =>
            {
                LoggingService.WriteLine($"Model with ID {Model.Id} was updated.");
                RaisePropertyChanged(nameof(Model));
            };
            Model.Tags.CollectionChanged += (s, e) =>
            {
                LoggingService.WriteLine($"Tags of model with ID {Model.Id} were updated.");
                RaisePropertyChanged(nameof(TagsString));
                RaisePropertyChanged(nameof(TagsAreExisting));
            };

            MarkAsReadCommand = new RelayCommand(() =>
            {
                LoggingService.WriteLine($"Marking item {Model.Id} as read.");
                Model.IsRead = true;
                UpdateItem();
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.MarkAsRead);
            });
            UnmarkAsReadCommand = new RelayCommand(() =>
            {
                LoggingService.WriteLine($"Marking item {Model.Id} as unread.");
                Model.IsRead = false;
                UpdateItem();
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.UnmarkAsRead);
            });
            MarkAsStarredCommand = new RelayCommand(() =>
            {
                LoggingService.WriteLine($"Marking item {Model.Id} as favorite.");
                Model.IsStarred = true;
                UpdateItem();
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.MarkAsStarred);
            });
            UnmarkAsStarredCommand = new RelayCommand(() =>
            {
                LoggingService.WriteLine($"Marking item {Model.Id} as unfavorite.");
                Model.IsStarred = false;
                UpdateItem();
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.UnmarkAsStarred);
            });
            DeleteCommand = new RelayCommand(() =>
            {
                LoggingService.WriteLine($"Deleting item {Model.Id}.");
                Database.Delete(Model);
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.Delete);
            });
            ShareCommand = new RelayCommand(() =>
            {
                LoggingService.WriteLine($"Sharing item {Model.Id}.");
                DataTransferManager.GetForCurrentView().DataRequested += (s, args) =>
                {
                    var data = args.Request.Data;

                    data.SetWebLink(new Uri(Model.Url));
                    data.Properties.Title = Model.Title;
                };
                DataTransferManager.ShowShareUI();
            });
            EditTagsCommand = new RelayCommand(async () =>
            {
                LoggingService.WriteLine($"Editing tags of item {Model.Id}.");
                await DialogService.ShowAsync(Dialogs.EditTagsDialog, new EditTagsViewModel(this.Model));
            });
            OpenInBrowserCommand = new RelayCommand(async () =>
            {
                LoggingService.WriteLine($"Opening item {Model.Id} in browser.");
                await Launcher.LaunchUriAsync(new Uri(Model.Url));
            });
        }

        public static ItemViewModel FromId(int itemId)
        {
            var ls = SimpleIoc.Default.GetInstance<ILoggingService>();
            ls.WriteLine($"Creating ItemViewModel from item id: {itemId}");

            var item = Database.Find<Item>(itemId);
            ls.WriteLineIf(item == null, $"Failed! Item does not exist in database!", LoggingCategory.Critical);

            if (item != null)
                return new ItemViewModel(item);
            else
                return null;
        }

        private void UpdateItem()
        {
            LoggingService.WriteLine($"Updating item {Model.Id} in database.");
            Model.LastModificationDate = DateTime.UtcNow;
            Database.Update(Model);
        }

        public int CompareTo(object obj) => ((IComparable)Model).CompareTo((obj as ItemViewModel).Model);
        public override bool Equals(object obj) => Model.Equals((obj as ItemViewModel).Model);
        public override int GetHashCode() => Model.GetHashCode();
    }
}
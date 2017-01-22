using GalaSoft.MvvmLight.Command;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Windows.Input;
using wallabag.Data.Models;
using wallabag.Data.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace wallabag.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class ItemViewModel : ViewModelBase, IComparable
    {
        public Item Model { get; private set; }

        public string TagsString { get { return string.Join(", ", Model.Tags); } }
        public bool TagsAreExisting { get { return Model.Tags.Count > 0; } }

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
            this.Model = Model;

            (Model as INotifyPropertyChanged).PropertyChanged += (s, e) => { RaisePropertyChanged(nameof(Model)); };
            Model.Tags.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(TagsString));
                RaisePropertyChanged(nameof(TagsAreExisting));
            };

            MarkAsReadCommand = new RelayCommand(() =>
            {
                Model.IsRead = true;
                UpdateItem();
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.MarkAsRead);
            });
            UnmarkAsReadCommand = new RelayCommand(() =>
            {
                Model.IsRead = false;
                UpdateItem();
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.UnmarkAsRead);
            });
            MarkAsStarredCommand = new RelayCommand(() =>
            {
                Model.IsStarred = true;
                UpdateItem();
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.MarkAsStarred);
            });
            UnmarkAsStarredCommand = new RelayCommand(() =>
            {
                Model.IsStarred = false;
                UpdateItem();
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.UnmarkAsStarred);
            });
            DeleteCommand = new RelayCommand(() =>
            {
                Database.Delete(Model);
                OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.Delete);
            });
            ShareCommand = new RelayCommand(() =>
            {
                DataTransferManager.GetForCurrentView().DataRequested += (s, args) =>
                {
                    var data = args.Request.Data;

                    data.SetWebLink(new Uri(Model.Url));
                    data.Properties.Title = Model.Title;
                };
                DataTransferManager.ShowShareUI();
            });
            EditTagsCommand = new RelayCommand(async () => await Services.DialogService.ShowAsync(Services.DialogService.Dialog.EditTags, new EditTagsViewModel(this.Model)));
            OpenInBrowserCommand = new RelayCommand(async () => await Launcher.LaunchUriAsync(new Uri(Model.Url)));
        }

        public static ItemViewModel FromId(int itemId)
        {
            var item = Database.Find<Item>(itemId);
            if (item != null)
                return new ItemViewModel(item);
            else
                return null;
        }

        private void UpdateItem()
        {
            Model.LastModificationDate = DateTime.UtcNow;
            Database.Update(Model);
        }

        public int CompareTo(object obj) => ((IComparable)Model).CompareTo((obj as ItemViewModel).Model);
        public override bool Equals(object obj) => Model.Equals((obj as ItemViewModel).Model);
        public override int GetHashCode() => Model.GetHashCode();
    }
}
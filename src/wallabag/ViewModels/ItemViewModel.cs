using PropertyChanged;
using System;
using System.ComponentModel;
using Template10.Mvvm;
using wallabag.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class ItemViewModel : ViewModelBase, IComparable
    {
        public Item Model { get; private set; }

        public string TagsString { get { return string.Join(", ", Model.Tags); } }
        public bool TagsAreExisting { get { return Model.Tags.Count > 0; } }

        public DelegateCommand MarkAsReadCommand { get; private set; }
        public DelegateCommand UnmarkAsReadCommand { get; private set; }
        public DelegateCommand MarkAsStarredCommand { get; private set; }
        public DelegateCommand UnmarkAsStarredCommand { get; private set; }
        public DelegateCommand DeleteCommand { get; private set; }
        public DelegateCommand ShareCommand { get; private set; }
        public DelegateCommand EditTagsCommand { get; private set; }
        public DelegateCommand OpenInBrowserCommand { get; private set; }

        public ItemViewModel(Item Model)
        {
            this.Model = Model;

            (Model as INotifyPropertyChanged).PropertyChanged += (s, e) => { RaisePropertyChanged(nameof(Model)); };
            Model.Tags.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(TagsString));
                RaisePropertyChanged(nameof(TagsAreExisting));
            };

            MarkAsReadCommand = new DelegateCommand(() =>
            {
                Model.IsRead = true;
                UpdateItem();
                OfflineTask.Add(Model.Id, OfflineTask.OfflineTaskAction.MarkAsRead);
            });
            UnmarkAsReadCommand = new DelegateCommand(() =>
            {
                Model.IsRead = false;
                UpdateItem();
                OfflineTask.Add(Model.Id, OfflineTask.OfflineTaskAction.UnmarkAsRead);
            });
            MarkAsStarredCommand = new DelegateCommand(() =>
            {
                Model.IsStarred = true;
                UpdateItem();
                OfflineTask.Add(Model.Id, OfflineTask.OfflineTaskAction.MarkAsStarred);
            });
            UnmarkAsStarredCommand = new DelegateCommand(() =>
            {
                Model.IsStarred = false;
                UpdateItem();
                OfflineTask.Add(Model.Id, OfflineTask.OfflineTaskAction.UnmarkAsStarred);
            });
            DeleteCommand = new DelegateCommand(() =>
            {
                App.Database.Delete(Model);
                OfflineTask.Add(Model.Id, OfflineTask.OfflineTaskAction.Delete);
            });
            ShareCommand = new DelegateCommand(() =>
            {
                DataTransferManager.GetForCurrentView().DataRequested += (s, args) =>
                {
                    var data = args.Request.Data;

                    data.SetWebLink(new Uri(Model.Url));
                    data.Properties.Title = Model.Title;
                };
                DataTransferManager.ShowShareUI();
            });
            EditTagsCommand = new DelegateCommand(async () => await Services.DialogService.ShowAsync(Services.DialogService.Dialog.EditTags, new EditTagsViewModel(this.Model)));
            OpenInBrowserCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(new Uri(Model.Url)));
        }

        public static ItemViewModel FromId(int itemId)
        {
            var item = App.Database.Find<Item>(itemId);
            if (item != null)
                return new ItemViewModel(item);
            else
                return null;
        }

        private void UpdateItem()
        {
            Model.LastModificationDate = DateTime.UtcNow;
            App.Database.Update(Model);
        }

        public int CompareTo(object obj) => ((IComparable)Model).CompareTo((obj as ItemViewModel).Model);
        public override bool Equals(object obj) => Model.Equals((obj as ItemViewModel).Model);
        public override int GetHashCode() => Model.GetHashCode();
    }
}
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Common.Helpers;
using wallabag.Models;
using wallabag.Services;
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

            MarkAsReadCommand = new DelegateCommand(async () =>
            {
                Model.IsRead = true;
                await UpdateItemAsync(OfflineTask.OfflineTaskAction.MarkAsRead);
            });
            UnmarkAsReadCommand = new DelegateCommand(async () =>
            {
                Model.IsRead = false;
                await UpdateItemAsync(OfflineTask.OfflineTaskAction.UnmarkAsRead);
            });
            MarkAsStarredCommand = new DelegateCommand(async () =>
            {
                Model.IsStarred = true;
                await UpdateItemAsync(OfflineTask.OfflineTaskAction.MarkAsStarred);
            });
            UnmarkAsStarredCommand = new DelegateCommand(async () =>
            {
                Model.IsStarred = false;
                await UpdateItemAsync(OfflineTask.OfflineTaskAction.UnmarkAsStarred);
            });
            DeleteCommand = new DelegateCommand(async () =>
            {
                await App.Database.RunInTransactionWithUndoAsync(() =>
                {
                    App.Database.Delete(Model);
                    OfflineTaskService.Add(Model.Id, OfflineTask.OfflineTaskAction.Delete);
                }, OfflineTask.OfflineTaskAction.Delete);
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

        private Task UpdateItemAsync(OfflineTask.OfflineTaskAction taskAction)
        {
            return App.Database.RunInTransactionWithUndoAsync(() =>
            {
                Model.LastModificationDate = DateTime.UtcNow;
                App.Database.Update(Model);
                OfflineTaskService.Add(Model.Id, taskAction);
            }, taskAction);
        }

        public int CompareTo(object obj) => ((IComparable)Model).CompareTo((obj as ItemViewModel).Model);
        public override bool Equals(object obj) => Model.Equals((obj as ItemViewModel).Model);
        public override int GetHashCode() => Model.GetHashCode();
    }
}
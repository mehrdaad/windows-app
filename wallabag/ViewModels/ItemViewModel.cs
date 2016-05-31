using GalaSoft.MvvmLight.Messaging;
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

            MarkAsReadCommand = new DelegateCommand(async () =>
            {
                if (await App.Client.ArchiveAsync(Model))
                {
                    Model.IsRead = true;
                    UpdateItem();
                }
            });
            UnmarkAsReadCommand = new DelegateCommand(async () =>
            {
                if (await App.Client.UnarchiveAsync(Model))
                {
                    Model.IsRead = false;
                    UpdateItem();
                }
            });
            MarkAsStarredCommand = new DelegateCommand(async () =>
            {
                if (await App.Client.FavoriteAsync(Model))
                {
                    Model.IsStarred = true;
                    UpdateItem();
                }
            });
            UnmarkAsStarredCommand = new DelegateCommand(async () =>
            {
                if (await App.Client.UnfavoriteAsync(Model))
                {
                    Model.IsStarred = false;
                    UpdateItem();
                }
            });
            DeleteCommand = new DelegateCommand(async () =>
            {
                if (await App.Client.DeleteAsync(Model))
                {
                    App.Database.Delete(Model);
                    SendUpdateMessage();
                }
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

        private void UpdateItem()
        {
            Model.LastModificationDate = DateTime.UtcNow;
            App.Database.Update(Model);
            SendUpdateMessage();
        }
        private void SendUpdateMessage() => Messenger.Default.Send(new NotificationMessage("FetchFromDatabase"));

        public int CompareTo(object obj) => ((IComparable)Model).CompareTo((obj as ItemViewModel).Model);
        public override bool Equals(object obj) => Model.Equals((obj as ItemViewModel).Model);
        public override int GetHashCode() => Model.GetHashCode();
    }
}
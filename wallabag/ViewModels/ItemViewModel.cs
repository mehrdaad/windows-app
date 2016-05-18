using PropertyChanged;
using System;
using Template10.Mvvm;
using wallabag.Api.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class ItemViewModel : ViewModelBase, IComparable
    {
        public WallabagItem Model { get; private set; }

        public DelegateCommand MarkAsReadCommand { get; private set; }
        public DelegateCommand UnmarkAsReadCommand { get; private set; }
        public DelegateCommand MarkAsStarredCommand { get; private set; }
        public DelegateCommand UnmarkAsStarredCommand { get; private set; }
        public DelegateCommand DeleteCommand { get; private set; }
        public DelegateCommand ShareCommand { get; private set; }
        public DelegateCommand EditTagsCommand { get; private set; }
        public DelegateCommand OpenInBrowserCommand { get; private set; }

        public ItemViewModel(WallabagItem Model)
        {
            this.Model = Model;

            MarkAsReadCommand = new DelegateCommand(async () => await App.Client.ArchiveAsync(Model));
            UnmarkAsReadCommand = new DelegateCommand(async () => await App.Client.UnarchiveAsync(Model));
            MarkAsStarredCommand = new DelegateCommand(async () => await App.Client.FavoriteAsync(Model));
            UnmarkAsStarredCommand = new DelegateCommand(async () => await App.Client.UnfavoriteAsync(Model));
            DeleteCommand = new DelegateCommand(async () => await App.Client.DeleteAsync(Model));
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
            EditTagsCommand = new DelegateCommand(async () => await Services.DialogService.ShowAsync(Services.DialogService.Dialog.EditTags, Model));
            OpenInBrowserCommand = new DelegateCommand(async () => await Launcher.LaunchUriAsync(new Uri(Model.Url)));
        }

        public int CompareTo(object obj) => ((IComparable)Model).CompareTo((obj as ItemViewModel).Model);
    }
}
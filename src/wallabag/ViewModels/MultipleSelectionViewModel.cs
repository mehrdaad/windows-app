using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System.Collections.Generic;
using Template10.Mvvm;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class MultipleSelectionViewModel
    {
        public List<ItemViewModel> Items { get; set; }

        public DelegateCommand MarkAsReadCommand { get; private set; }
        public DelegateCommand UnmarkAsReadCommand { get; private set; }
        public DelegateCommand MarkAsFavoriteCommand { get; private set; }
        public DelegateCommand UnmarkAsFavoriteCommand { get; private set; }
        public DelegateCommand EditTagsCommand { get; private set; }
        public DelegateCommand OpenInBrowserCommand { get; private set; }
        public DelegateCommand DeleteCommand { get; private set; }

        public MultipleSelectionViewModel()
        {
            Items = new List<ItemViewModel>();

            MarkAsReadCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.MarkAsReadCommand.Execute();

                CompleteMultipleSelection();
            });
            UnmarkAsReadCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.UnmarkAsReadCommand.Execute();

                CompleteMultipleSelection();
            });
            MarkAsFavoriteCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.MarkAsStarredCommand.Execute();

                CompleteMultipleSelection();
            });
            UnmarkAsFavoriteCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.UnmarkAsStarredCommand.Execute();

                CompleteMultipleSelection();
            });
            EditTagsCommand = new DelegateCommand(async () =>
            {
                var viewModel = new EditTagsViewModel();

                foreach (var item in Items)
                    viewModel.Items.Add(item.Model);

                await Services.DialogService.ShowAsync(Services.DialogService.Dialog.EditTags, viewModel);
            });
            OpenInBrowserCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.OpenInBrowserCommand.Execute();
            });
            DeleteCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.DeleteCommand.Execute();

                CompleteMultipleSelection();
            });
        }

        public void CompleteMultipleSelection() => Messenger.Default.Send(new NotificationMessage("CompleteMultipleSelection"));
    }
}

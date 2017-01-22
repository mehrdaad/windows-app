using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Common.Messages;

namespace wallabag.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class MultipleSelectionViewModel : ViewModelBase
    {
        public List<ItemViewModel> Items { get; set; }

        public ICommand MarkAsReadCommand { get; private set; }
        public ICommand UnmarkAsReadCommand { get; private set; }
        public ICommand MarkAsFavoriteCommand { get; private set; }
        public ICommand UnmarkAsFavoriteCommand { get; private set; }
        public ICommand EditTagsCommand { get; private set; }
        public ICommand OpenInBrowserCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public MultipleSelectionViewModel()
        {
            Items = new List<ItemViewModel>();

            MarkAsReadCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                Database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.MarkAsReadCommand.Execute();
                });
            }));
            UnmarkAsReadCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                Database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.UnmarkAsReadCommand.Execute();
                });
            }));
            MarkAsFavoriteCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                Database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.MarkAsStarredCommand.Execute();
                });
            }));
            UnmarkAsFavoriteCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                Database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.UnmarkAsStarredCommand.Execute();
                });
            }));
            EditTagsCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(async () =>
            {
                var viewModel = new EditTagsViewModel();

                foreach (var item in Items)
                    viewModel.Items.Add(item.Model);

                await Services.DialogService.ShowAsync(Services.DialogService.Dialog.EditTags, viewModel);
            }));
            OpenInBrowserCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                Database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.OpenInBrowserCommand.Execute();
                });
            }));
            DeleteCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                Database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.DeleteCommand.Execute();
                });
            }));
        }

        private void ExecuteMultipleSelectionAction(Action a)
        {
            a.Invoke();
            Messenger.Default.Send(new CompleteMultipleSelectionMessage());
        }
    }
}

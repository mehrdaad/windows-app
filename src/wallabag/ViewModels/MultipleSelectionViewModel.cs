using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using Template10.Mvvm;
using wallabag.Common.Messages;

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

            MarkAsReadCommand = new DelegateCommand(() => BlockOfflineTaskExecution(() =>
            {
                foreach (var item in Items)
                    item.MarkAsReadCommand.Execute();
            }));
            UnmarkAsReadCommand = new DelegateCommand(() => BlockOfflineTaskExecution(() =>
            {
                foreach (var item in Items)
                    item.UnmarkAsReadCommand.Execute();
            }));
            MarkAsFavoriteCommand = new DelegateCommand(() => BlockOfflineTaskExecution(() =>
            {
                foreach (var item in Items)
                    item.MarkAsStarredCommand.Execute();
            }));
            UnmarkAsFavoriteCommand = new DelegateCommand(() => BlockOfflineTaskExecution(() =>
            {
                foreach (var item in Items)
                    item.UnmarkAsStarredCommand.Execute();
            }));
            EditTagsCommand = new DelegateCommand(() => BlockOfflineTaskExecution(async () =>
            {
                var viewModel = new EditTagsViewModel();

                foreach (var item in Items)
                    viewModel.Items.Add(item.Model);

                await Services.DialogService.ShowAsync(Services.DialogService.Dialog.EditTags, viewModel);
            }));
            OpenInBrowserCommand = new DelegateCommand(() => BlockOfflineTaskExecution(() =>
            {
                foreach (var item in Items)
                    item.OpenInBrowserCommand.Execute();
            }));
            DeleteCommand = new DelegateCommand(() => BlockOfflineTaskExecution(() =>
            {
                foreach (var item in Items)
                    item.DeleteCommand.Execute();
            }));
        }

        private void BlockOfflineTaskExecution(Action a)
        {
            Messenger.Default.Send(new BlockOfflineTaskExecutionMessage(true));
            a.Invoke();
            Messenger.Default.Send(new BlockOfflineTaskExecutionMessage(false));
        }
    }
}

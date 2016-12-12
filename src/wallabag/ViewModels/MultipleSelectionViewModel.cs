using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Common.Helpers;
using wallabag.Common.Messages;
using wallabag.Models;
using wallabag.Services;

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
        public DelegateCommand DeleteCommand { get; private set; }
        public DelegateCommand EditTagsCommand { get; private set; }
        public DelegateCommand OpenInBrowserCommand { get; private set; }

        public MultipleSelectionViewModel()
        {
            Items = new List<ItemViewModel>();

            MarkAsReadCommand = new DelegateCommand(async () => await ExecuteActionOnItemsAsync(OfflineTask.OfflineTaskAction.MarkAsRead));
            UnmarkAsReadCommand = new DelegateCommand(async () => await ExecuteActionOnItemsAsync(OfflineTask.OfflineTaskAction.UnmarkAsRead));
            MarkAsFavoriteCommand = new DelegateCommand(async () => await ExecuteActionOnItemsAsync(OfflineTask.OfflineTaskAction.MarkAsStarred));
            UnmarkAsFavoriteCommand = new DelegateCommand(async () => await ExecuteActionOnItemsAsync(OfflineTask.OfflineTaskAction.UnmarkAsStarred));
            DeleteCommand = new DelegateCommand(async () => await ExecuteActionOnItemsAsync(OfflineTask.OfflineTaskAction.Delete));
            EditTagsCommand = new DelegateCommand(async () => await App.Database.RunInTransactionWithUndoAsync(async () =>
            {
                var viewModel = new EditTagsViewModel();

                foreach (var item in Items)
                    viewModel.Items.Add(item.Model);

                await DialogService.ShowAsync(DialogService.Dialog.EditTags, viewModel);
            }, OfflineTask.OfflineTaskAction.EditTags, Items.Count));
            OpenInBrowserCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.OpenInBrowserCommand.Execute();
            });
        }

        private Task ExecuteActionOnItemsAsync(OfflineTask.OfflineTaskAction action)
        {
            return App.Database.RunInTransactionWithUndoAsync(() =>
            {
                foreach (var item in Items)
                {
                    switch (action)
                    {
                        case OfflineTask.OfflineTaskAction.MarkAsRead:
                            item.MarkAsReadCommand.Execute();
                            break;
                        case OfflineTask.OfflineTaskAction.UnmarkAsRead:
                            item.UnmarkAsReadCommand.Execute();
                            break;
                        case OfflineTask.OfflineTaskAction.MarkAsStarred:
                            item.MarkAsStarredCommand.Execute();
                            break;
                        case OfflineTask.OfflineTaskAction.UnmarkAsStarred:
                            item.UnmarkAsStarredCommand.Execute();
                            break;
                        case OfflineTask.OfflineTaskAction.Delete:
                            item.DeleteCommand.Execute();
                            break;
                    }
                }
            }, action, Items.Count);
        }
    }
}

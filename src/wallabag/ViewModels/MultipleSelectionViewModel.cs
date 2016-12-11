using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
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

            MarkAsReadCommand = new DelegateCommand(() => ExecuteActionOnItems(OfflineTask.OfflineTaskAction.MarkAsRead));
            UnmarkAsReadCommand = new DelegateCommand(() => ExecuteActionOnItems(OfflineTask.OfflineTaskAction.UnmarkAsRead));
            MarkAsFavoriteCommand = new DelegateCommand(() => ExecuteActionOnItems(OfflineTask.OfflineTaskAction.MarkAsStarred));
            UnmarkAsFavoriteCommand = new DelegateCommand(() => ExecuteActionOnItems(OfflineTask.OfflineTaskAction.UnmarkAsStarred));
            DeleteCommand = new DelegateCommand(() => ExecuteActionOnItems(OfflineTask.OfflineTaskAction.Delete));
            EditTagsCommand = new DelegateCommand(() => BlockOfflineTaskExecution(async () =>
            {
                var viewModel = new EditTagsViewModel();

                foreach (var item in Items)
                    viewModel.Items.Add(item.Model);

                await Services.DialogService.ShowAsync(Services.DialogService.Dialog.EditTags, viewModel);
            }));
            OpenInBrowserCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.OpenInBrowserCommand.Execute();
            });
        }

        private void BlockOfflineTaskExecution(Action a)
        {
            OfflineTaskService.IsBlocked = true;
            a.Invoke();
            OfflineTaskService.IsBlocked = false;
            Messenger.Default.Send(new CompleteMultipleSelectionMessage());
        }

        private void ExecuteActionOnItems(OfflineTask.OfflineTaskAction action)
        {
            BlockOfflineTaskExecution(async () =>
            {
                await App.Database.RunInTransactionWithUndoAsync(() =>
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
            });
        }
    }
}

using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using wallabag.Data.Common;
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
            _loggingService.WriteLine("Creating new instance of MultipleSelectionViewModel.");
            Items = new List<ItemViewModel>();

            MarkAsReadCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                _loggingService.WriteLine($"Marking {Items.Count} items as read...");
                _database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.MarkAsReadCommand.Execute();
                });
            }));
            UnmarkAsReadCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                _loggingService.WriteLine($"Marking {Items.Count} items as unread...");
                _database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.UnmarkAsReadCommand.Execute();
                });
            }));
            MarkAsFavoriteCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                _loggingService.WriteLine($"Marking {Items.Count} items as favorite...");
                _database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.MarkAsStarredCommand.Execute();
                });
            }));
            UnmarkAsFavoriteCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                _loggingService.WriteLine($"Marking {Items.Count} items as unfavorited...");
                _database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.UnmarkAsStarredCommand.Execute();
                });
            }));
            EditTagsCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                _loggingService.WriteLine($"Editing tags of {Items.Count} items...");

                var viewModel = new EditTagsViewModel();

                foreach (var item in Items)
                    viewModel.Items.Add(item.Model);

                _navigationService.Navigate(Navigation.Pages.EditTagsPage, viewModel);
            }));
            OpenInBrowserCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                _loggingService.WriteLine($"Opening {Items.Count} items in the browser...");

                foreach (var item in Items)
                    item.OpenInBrowserCommand.Execute();
            }));
            DeleteCommand = new RelayCommand(() => ExecuteMultipleSelectionAction(() =>
            {
                _loggingService.WriteLine($"Deleting {Items.Count} items...");

                _database.RunInTransaction(() =>
                {
                    foreach (var item in Items)
                        item.DeleteCommand.Execute();
                });
            }));
        }

        private void ExecuteMultipleSelectionAction(Action a)
        {
            _loggingService.WriteLine($"Executing multiple selection action...");
            a.Invoke();
            Messenger.Default.Send(new CompleteMultipleSelectionMessage());
            _loggingService.WriteLine($"Multiple selection action completed.");
        }
    }
}

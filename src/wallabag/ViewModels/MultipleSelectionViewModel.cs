using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Models;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class MultipleSelectionViewModel
    {
        public IList<ItemViewModel> Items { get; set; }

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
            });
            UnmarkAsReadCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.UnmarkAsReadCommand.Execute();
            });
            MarkAsFavoriteCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.MarkAsStarredCommand.Execute();
            });
            UnmarkAsFavoriteCommand = new DelegateCommand(() =>
            {
                foreach (var item in Items)
                    item.UnmarkAsStarredCommand.Execute();
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
            });
        }
    }
}

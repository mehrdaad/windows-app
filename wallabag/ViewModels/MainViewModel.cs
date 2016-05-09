using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        public ICollection<ItemViewModel> Items { get; set; } = new ObservableCollection<ItemViewModel>();

        public DelegateCommand SyncCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }

        public MainViewModel()
        {
            AddCommand = new DelegateCommand(async () => await new Dialogs.AddItemDialog().ShowAsync());
            SyncCommand = new DelegateCommand(async () => await SyncAsync());
        }

        private async Task SyncAsync()
        {
            var items = (await App.Client.GetItemsAsync(DateOrder: Api.WallabagClient.WallabagDateOrder.ByLastModificationDate)).ToList();

            foreach (var item in items)
                Items.Add(new ItemViewModel(item));

            await App.Client.GetAccessTokenAsync();
            await Task.Factory.StartNew(() => App.Database.InsertOrReplaceAll(items));
        }
    }
}
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        public ICollection<ItemViewModel> Items { get; set; } = new ObservableCollection<ItemViewModel>();

        public DelegateCommand SyncCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand NavigateToSettingsPageCommand { get; private set; }
        public DelegateCommand<ItemClickEventArgs> ItemClickCommand { get; private set; }

        public MainViewModel()
        {
            AddCommand = new DelegateCommand(async () => await Services.DialogService.ShowAsync(Services.DialogService.Dialog.AddItem));
            SyncCommand = new DelegateCommand(async () => await SyncAsync());
            NavigateToSettingsPageCommand = new DelegateCommand(() => NavigationService.Navigate(typeof(Views.SettingsPage), infoOverride: new DrillInNavigationTransitionInfo()));
            ItemClickCommand = new DelegateCommand<ItemClickEventArgs>(t => ItemClick(t));
        }

        private async Task SyncAsync()
        {
            var items = (await App.Client.GetItemsAsync(DateOrder: Api.WallabagClient.WallabagDateOrder.ByLastModificationDate)).ToList();

            foreach (var item in items)
                Items.Add(new ItemViewModel(item));

            await App.Client.GetAccessTokenAsync();
            await Task.Factory.StartNew(() => App.Database.InsertOrReplaceAll(items));
        }

        private void ItemClick(ItemClickEventArgs args)
        {
            var item = args.ClickedItem as ItemViewModel;
            NavigationService.Navigate(typeof(Views.ItemPage), item);
        }
    }
}
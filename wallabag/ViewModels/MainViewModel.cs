using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Api.Models;
using wallabag.Common;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        private List<WallabagItem> _items = new List<WallabagItem>();
        public ObservableCollection<ItemViewModel> Items { get; set; } = new ObservableCollection<ItemViewModel>();

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
            var items = (await App.Client.GetItemsAsync()).ToList();

            foreach (var item in items)
            {
                if (!_items.Contains(item))
                {
                    _items.Add(item);
                    Items.Add(new ItemViewModel(item));
                }
            }

            await App.Client.GetAccessTokenAsync();
            await Task.Factory.StartNew(() => App.Database.InsertOrReplaceAll(items));
        }
        private void FetchFromDatabase()
        {
            var databaseItems = App.Database.Table<WallabagItem>().ToList();

            var newItems = databaseItems.Except(_items);
            var changedItems = databaseItems.Except(_items).Except(newItems);
            var deletedItems = _items.Except(databaseItems);

            _items = databaseItems;

            foreach (var item in newItems)
                Items.AddSorted(new ItemViewModel(item));

            foreach (var item in changedItems)
            {
                Items.Remove(Items.Where(i => i.Model.Id == item.Id).First());
                Items.AddSorted(new ItemViewModel(item));           
            }

            foreach (var item in deletedItems)
                Items.Remove(new ItemViewModel(item));
        }
        private void ItemClick(ItemClickEventArgs args)
        {
            var item = args.ClickedItem as ItemViewModel;
            NavigationService.Navigate(typeof(Views.ItemPage), item.Model);
        }

        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if (mode != NavigationMode.Refresh)
                FetchFromDatabase();

            Messenger.Default.Register<NotificationMessage>(this, message =>
            {
                if (message.Notification.Equals("FetchFromDatabase"))
                    FetchFromDatabase();
            });
            return base.OnNavigatedToAsync(parameter, mode, state);
        }
    }
}
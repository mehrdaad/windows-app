using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Common;
using wallabag.Models;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        private List<Item> _items = new List<Item>();
        public ObservableCollection<ItemViewModel> Items { get; set; } = new ObservableCollection<ItemViewModel>();

        public DelegateCommand SyncCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand NavigateToSettingsPageCommand { get; private set; }
        public DelegateCommand<ItemClickEventArgs> ItemClickCommand { get; private set; }

        public SearchProperties CurrentSearchProperties { get; private set; } = new SearchProperties(); 
        public DelegateCommand<string> SetItemTypeFilterCommand { get; private set; }
        public DelegateCommand<string> SetEstimatedReadingTimeFilterCommand { get; private set; }
        public DelegateCommand<string> SetCreationDateFilterCommand { get; private set; }

        public MainViewModel()
        {
            AddCommand = new DelegateCommand(async () => await Services.DialogService.ShowAsync(Services.DialogService.Dialog.AddItem));
            SyncCommand = new DelegateCommand(async () => await SyncAsync());
            NavigateToSettingsPageCommand = new DelegateCommand(() => NavigationService.Navigate(typeof(Views.SettingsPage), infoOverride: new DrillInNavigationTransitionInfo()));
            ItemClickCommand = new DelegateCommand<ItemClickEventArgs>(t => ItemClick(t));

            SetItemTypeFilterCommand = new DelegateCommand<string>(type => SetItemTypeFilter(type));
            SetEstimatedReadingTimeFilterCommand = new DelegateCommand<string>(order => SetEstimatedReadingTimeFilter(order));
            SetCreationDateFilterCommand = new DelegateCommand<string>(order => SetCreationDateFilter(order));
        }

        private async Task SyncAsync()
        {
            var items = await App.Client.GetItemsAsync();

            if (items != null)
            {
                foreach (var item in items)
                    if (!_items.Contains(item))
                        _items.Add(item);

                await Task.Factory.StartNew(() => App.Database.InsertOrReplaceAll(_items));
                FetchFromDatabase();
            }
        }
        private void FetchFromDatabase()
        {
            var databaseItems = App.Database.Table<Item>().Where(i => i.IsRead == false).ToList();

            var idComparer = new ItemByIdEqualityComparer();
            var modificationDateComparer = new ItemByModificationDateEqualityComparer();

            var newItems = databaseItems.Except(_items, idComparer);
            var changedItems = databaseItems.Except(_items, modificationDateComparer).Except(newItems);
            var deletedItems = _items.Except(databaseItems, idComparer);

            _items = databaseItems;

            foreach (var item in newItems)
                Items.AddSorted(new ItemViewModel(item));

            foreach (var item in changedItems)
            {
                Items.Remove(Items.Where(i => i.Model.Id == item.Id).FirstOrDefault());
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

        private void SetItemTypeFilter(string type)
        {
            switch (type)
            {
                case "all":
                    CurrentSearchProperties.ItemType = SearchProperties.SearchPropertiesItemType.All;
                    break;
                case "unread":
                    CurrentSearchProperties.ItemType = SearchProperties.SearchPropertiesItemType.Unread;
                    break;
                case "starred":
                    CurrentSearchProperties.ItemType = SearchProperties.SearchPropertiesItemType.Favorites;
                    break;
                case "archived":
                    CurrentSearchProperties.ItemType = SearchProperties.SearchPropertiesItemType.Archived;
                    break;
            }
        }
        private void SetEstimatedReadingTimeFilter(string order)
        {
            if (order.Equals("asc"))
                CurrentSearchProperties.ReadingTimeSortOrder = SearchProperties.SortOrder.Ascending;
            else
                CurrentSearchProperties.ReadingTimeSortOrder = SearchProperties.SortOrder.Descending;
        }
        private void SetCreationDateFilter(string order)
        {
            if (order.Equals("asc"))
                CurrentSearchProperties.CreationDateSortOrder = SearchProperties.SortOrder.Ascending;
            else
                CurrentSearchProperties.CreationDateSortOrder = SearchProperties.SortOrder.Descending;
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
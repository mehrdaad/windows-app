using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
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
        public ObservableCollection<Item> SearchQuerySuggestions { get; set; } = new ObservableCollection<Item>();
        public ObservableCollection<Language> LanguageSuggestions { get; set; } = new ObservableCollection<Language>();
        public ObservableCollection<Tag> TagSuggestions { get; set; } = new ObservableCollection<Tag>();
        public DelegateCommand<string> SetItemTypeFilterCommand { get; private set; }
        public DelegateCommand<string> SetEstimatedReadingTimeFilterCommand { get; private set; }
        public DelegateCommand<string> SetCreationDateFilterCommand { get; private set; }
        public DelegateCommand<AutoSuggestBoxTextChangedEventArgs> SearchQueryChangedCommand { get; private set; }
        public DelegateCommand<AutoSuggestBoxQuerySubmittedEventArgs> SearchQuerySubmittedCommand { get; private set; }
        public DelegateCommand<SelectionChangedEventArgs> LanguageCodeChangedCommand { get; private set; }
        public DelegateCommand<SelectionChangedEventArgs> TagChangedCommand { get; private set; }
        public DelegateCommand ResetFilterCommand { get; private set; }

        public MainViewModel()
        {
            AddCommand = new DelegateCommand(async () => await Services.DialogService.ShowAsync(Services.DialogService.Dialog.AddItem));
            SyncCommand = new DelegateCommand(async () => await SyncAsync());
            NavigateToSettingsPageCommand = new DelegateCommand(() => NavigationService.Navigate(typeof(Views.SettingsPage), infoOverride: new DrillInNavigationTransitionInfo()));
            ItemClickCommand = new DelegateCommand<ItemClickEventArgs>(t => ItemClick(t));

            SetItemTypeFilterCommand = new DelegateCommand<string>(type => SetItemTypeFilter(type));
            SetEstimatedReadingTimeFilterCommand = new DelegateCommand<string>(order => SetEstimatedReadingTimeFilter(order));
            SetCreationDateFilterCommand = new DelegateCommand<string>(order => SetCreationDateFilter(order));
            SearchQueryChangedCommand = new DelegateCommand<AutoSuggestBoxTextChangedEventArgs>(args => SearchQueryChanged(args));
            SearchQuerySubmittedCommand = new DelegateCommand<AutoSuggestBoxQuerySubmittedEventArgs>(args => SearchQuerySubmitted(args));
            LanguageCodeChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(args => LanguageCodeChanged(args));
            TagChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(args => TagChanged(args));
            ResetFilterCommand = new DelegateCommand(() => CurrentSearchProperties.Reset());

            CurrentSearchProperties.SearchCanceled += p => FetchFromDatabase();
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
            UpdateItemCollection(databaseItems);
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
            UpdateViewBySearchProperties();
        }
        private void SetEstimatedReadingTimeFilter(string order)
        {
            if (order.Equals("asc"))
                CurrentSearchProperties.SortOrder = SearchProperties.SearchPropertiesSortOrder.AscendingByReadingTime;
            else
                CurrentSearchProperties.SortOrder = SearchProperties.SearchPropertiesSortOrder.DescendingByReadingTime;
            UpdateViewBySearchProperties();
        }
        private void SetCreationDateFilter(string order)
        {
            if (order.Equals("asc"))
                CurrentSearchProperties.SortOrder = SearchProperties.SearchPropertiesSortOrder.AscendingByCreationDate;
            else
                CurrentSearchProperties.SortOrder = SearchProperties.SearchPropertiesSortOrder.DescendingByCreationDate;
            UpdateViewBySearchProperties();
        }
        private void SearchQueryChanged(AutoSuggestBoxTextChangedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
                return;

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var suggestions = App.Database.Table<Item>().Where(i => i.Title.ToLower().Contains(CurrentSearchProperties.Query)).Take(5);
                SearchQuerySuggestions.Replace(suggestions.ToList());
            }
        }
        private void SearchQuerySubmitted(AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                NavigationService.Navigate(typeof(Views.ItemPage), args.ChosenSuggestion as Item);
                return;
            }

            if (string.IsNullOrWhiteSpace(args.QueryText))
                return;

            CurrentSearchProperties.ItemType = SearchProperties.SearchPropertiesItemType.All;
            UpdateViewBySearchProperties();
        }
        private void LanguageCodeChanged(SelectionChangedEventArgs args)
        {
            var selectedLanguage = args.AddedItems.FirstOrDefault() as Language;

            CurrentSearchProperties.Language = selectedLanguage as Language;
            UpdateViewBySearchProperties();
        }
        private void TagChanged(SelectionChangedEventArgs args)
        {
            var selectedTag = args.AddedItems.FirstOrDefault() as Tag;

            CurrentSearchProperties.Tag = selectedTag as Tag;
            UpdateViewBySearchProperties();
        }

        private void UpdateItemCollection(List<Item> newItemList)
        {
            var idComparer = new ItemByIdEqualityComparer();
            var modificationDateComparer = new ItemByModificationDateEqualityComparer();

            var newItems = newItemList.Except(_items, idComparer);
            var changedItems = newItemList.Except(_items, modificationDateComparer).Except(newItems);
            var deletedItems = _items.Except(newItemList, idComparer);

            _items = newItemList;

            foreach (var item in _items)
            {
                if (item.Language != null)
                {
                    var translatedLanguage = new Language(item.Language);

                    if (!LanguageSuggestions.Contains(translatedLanguage))
                        LanguageSuggestions.Add(translatedLanguage);
                }
                else
                {
                    if (!LanguageSuggestions.Contains(Language.Unknown))
                        LanguageSuggestions.Add(Language.Unknown);
                }

                foreach (var tag in item.Tags)
                    if (!TagSuggestions.Contains(tag))
                        TagSuggestions.Add(tag);
            }

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
        private void UpdateViewBySearchProperties()
        {
            var items = App.Database.Table<Item>();

            switch (CurrentSearchProperties.ItemType)
            {
                case SearchProperties.SearchPropertiesItemType.Unread:
                    items = items.Where(i => i.IsRead == false); break;
                case SearchProperties.SearchPropertiesItemType.Favorites:
                    items = items.Where(i => i.IsStarred == true); break;
                case SearchProperties.SearchPropertiesItemType.Archived:
                    items = items.Where(i => i.IsRead == true); break;
                case SearchProperties.SearchPropertiesItemType.All:
                default:
                    break;
            }

            if (!string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
                items = items.Where(i => i.Title.ToLower().Contains(CurrentSearchProperties.Query));

            if (CurrentSearchProperties.Language?.IsUnknown == false)
                items = items.Where(i => i.Language.Equals(CurrentSearchProperties.Language.wallabagLanguageCode));
            else if (CurrentSearchProperties.Language?.IsUnknown == true)
                items = items.Where(i => i.Language == null);

            var list = items.ToList();

            if (CurrentSearchProperties.Tag != null)
                list = list.Where(i => i.Tags.Contains(CurrentSearchProperties.Tag)).ToList();

            UpdateItemCollection(list);

            IOrderedEnumerable<ItemViewModel> sortedItems;

            switch (CurrentSearchProperties.SortOrder)
            {
                case SearchProperties.SearchPropertiesSortOrder.AscendingByReadingTime:
                    sortedItems = Items.OrderBy(i => i.Model.EstimatedReadingTime);
                    break;
                case SearchProperties.SearchPropertiesSortOrder.DescendingByReadingTime:
                    sortedItems = Items.OrderByDescending(i => i.Model.EstimatedReadingTime);
                    break;
                case SearchProperties.SearchPropertiesSortOrder.AscendingByCreationDate:
                    sortedItems = Items.OrderBy(i => i.Model.CreationDate);
                    break;
                case SearchProperties.SearchPropertiesSortOrder.DescendingByCreationDate:
                default:
                    sortedItems = Items.OrderByDescending(i => i.Model.CreationDate);
                    break;
            }

            Items = new ObservableCollection<ItemViewModel>(sortedItems);
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if (mode != NavigationMode.Refresh)
                FetchFromDatabase();

            Messenger.Default.Register<NotificationMessage>(this, message =>
            {
                if (message.Notification.Equals("FetchFromDatabase"))
                    FetchFromDatabase();
            });

            if (state.ContainsKey(nameof(CurrentSearchProperties)))
            {
                var stateValue = state[nameof(CurrentSearchProperties)] as string;
                CurrentSearchProperties = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<SearchProperties>(stateValue));
            }
        }
        public override async Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            var serializedSearchProperties = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(CurrentSearchProperties));
            pageState[nameof(CurrentSearchProperties)] = serializedSearchProperties;
        }
    }
}
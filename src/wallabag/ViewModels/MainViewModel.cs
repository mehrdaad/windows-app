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
using wallabag.Services;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        public IncrementalObservableCollection<ItemViewModel> Items { get; set; }

        [DependsOn(nameof(OfflineTaskCount))]
        public Visibility OfflineTaskVisibility { get { return OfflineTaskCount > 0 ? Visibility.Visible : Visibility.Collapsed; } }
        public int OfflineTaskCount { get; set; }
        public bool IsSyncing { get; set; }

        public bool ItemsCountIsZero { get { return Items.Count == 0; } }
        public bool IsSearchActive { get; set; } = false;
        public string PageHeader { get; set; } = Helpers.LocalizedResource("SearchBox.PlaceholderText").ToUpper();

        public bool? SortByCreationDate
        {
            get { return CurrentSearchProperties.SortType == SearchProperties.SearchPropertiesSortType.ByCreationDate; }
            set { CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByCreationDate; }
        }
        public bool? SortByReadingTime
        {
            get { return CurrentSearchProperties.SortType == SearchProperties.SearchPropertiesSortType.ByReadingTime; }
            set { CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByReadingTime; }
        }
        public DelegateCommand<string> SetSortTypeFilterCommand { get; private set; }
        public DelegateCommand<string> SetSortOrderCommand { get; private set; }

        public DelegateCommand SyncCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand NavigateToSettingsPageCommand { get; private set; }
        public DelegateCommand<SelectionChangedEventArgs> PivotSelectionChangedCommand { get; private set; }

        public SearchProperties CurrentSearchProperties { get; private set; } = new SearchProperties();
        public ObservableCollection<Item> SearchQuerySuggestions { get; set; } = new ObservableCollection<Item>();
        public ObservableCollection<Language> LanguageSuggestions { get; set; } = new ObservableCollection<Language>();
        public ObservableCollection<Tag> TagSuggestions { get; set; } = new ObservableCollection<Tag>();
        public DelegateCommand<AutoSuggestBoxTextChangedEventArgs> SearchQueryChangedCommand { get; private set; }
        public DelegateCommand<AutoSuggestBoxQuerySubmittedEventArgs> SearchQuerySubmittedCommand { get; private set; }
        public DelegateCommand CloseSearchCommand { get; private set; }
        public DelegateCommand<SelectionChangedEventArgs> LanguageCodeChangedCommand { get; private set; }
        public DelegateCommand<SelectionChangedEventArgs> TagChangedCommand { get; private set; }
        public DelegateCommand ResetFilterLanguageCommand { get; private set; }
        public DelegateCommand ResetFilterTagCommand { get; private set; }
        public DelegateCommand ResetFilterCommand { get; private set; }

        public MainViewModel()
        {
            AddCommand = new DelegateCommand(async () => await DialogService.ShowAsync(DialogService.Dialog.AddItem));
            SyncCommand = new DelegateCommand(async () => await SyncAsync());
            NavigateToSettingsPageCommand = new DelegateCommand(() => NavigationService.Navigate(typeof(Views.SettingsPage), infoOverride: new DrillInNavigationTransitionInfo()));

            SetSortTypeFilterCommand = new DelegateCommand<string>(filter => SetSortTypeFilter(filter));
            SetSortOrderCommand = new DelegateCommand<string>(order => SetSortOrder(order));
            SearchQueryChangedCommand = new DelegateCommand<AutoSuggestBoxTextChangedEventArgs>(args => SearchQueryChanged(args));
            SearchQuerySubmittedCommand = new DelegateCommand<AutoSuggestBoxQuerySubmittedEventArgs>(args => SearchQuerySubmitted(args));
            CloseSearchCommand = new DelegateCommand(() => EndSearch(this, null));
            LanguageCodeChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(args => LanguageCodeChanged(args));
            TagChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(args => TagChanged(args));
            ResetFilterLanguageCommand = new DelegateCommand(() => CurrentSearchProperties.Language = null);
            ResetFilterTagCommand = new DelegateCommand(() => CurrentSearchProperties.Tag = null);
            ResetFilterCommand = new DelegateCommand(() => CurrentSearchProperties.Reset());

            CurrentSearchProperties.SearchStarted += p => StartSearch();
            CurrentSearchProperties.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(CurrentSearchProperties.Query))
                    UpdateView();

                RaisePropertyChanged(nameof(SortByCreationDate));
                RaisePropertyChanged(nameof(SortByReadingTime));
            };

            Items = new IncrementalObservableCollection<ItemViewModel>(async count => await LoadMoreItemsAsync(count));

            App.OfflineTaskAdded += async (s, e) =>
            {
                OfflineTaskCount += 1;
                await e.ExecuteAsync();
                UpdateView();
            };
            App.OfflineTaskRemoved += (s, e) => OfflineTaskCount -= 1;
            Items.CollectionChanged += (s, e) => RaisePropertyChanged(nameof(ItemsCountIsZero));
        }

        private Task<List<ItemViewModel>> LoadMoreItemsAsync(uint count)
        {
            var result = new List<ItemViewModel>();

            var database = GetItemsForCurrentSearchProperties(Items.Count, (int)count);

            foreach (var item in database)
                result.Add(new ItemViewModel(item));

            GetMetadataForItems(result);

            return Task.FromResult(result);
        }

        private async Task ExecuteOfflineTasksAsync()
        {
            foreach (var task in App.Database.Table<OfflineTask>())
                await task.ExecuteAsync();

            OfflineTaskCount = App.Database.Table<OfflineTask>().Count();

            UpdateView();
        }
        private async Task SyncAsync()
        {
            if (Helpers.InternetConnectionIsAvailable == false)
                return;

            IsSyncing = true;
            await ExecuteOfflineTasksAsync();
            int syncLimit = 30;

            var items = await App.Client.GetItemsAsync(
                dateOrder: Api.WallabagClient.WallabagDateOrder.ByLastModificationDate,
                sortOrder: Api.WallabagClient.WallabagSortOrder.Descending,
                itemsPerPage: syncLimit);

            if (items != null)
            {
                var itemList = new List<Item>();

                foreach (var item in items)
                    itemList.Add(item);

                var databaseList = App.Database.Table<Item>()
                    .OrderByDescending(i => i.LastModificationDate)
                    .Take(syncLimit).ToList();
                var deletedItems = databaseList.Except(itemList);

                App.Database.RunInTransaction(() =>
                {
                    foreach (var item in deletedItems)
                        App.Database.Delete(item);

                    App.Database.InsertOrReplaceAll(itemList);
                });

                UpdateView();
            }
            IsSyncing = false;
        }

        internal void ItemClick(object sender, ItemClickEventArgs args)
        {
            var item = args.ClickedItem as ItemViewModel;
            NavigationService.Navigate(typeof(Views.ItemPage), item.Model.Id);
        }

        private void UpdatePageHeader()
        {
            if (IsSearchActive)
                PageHeader = string.Format(Helpers.LocalizedResource("SearchPivotItem.Header").ToUpper(), "\"" + CurrentSearchProperties.Query + "\"");
            else
                PageHeader = Helpers.LocalizedResource("SearchBox.PlaceholderText").ToUpper();
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
                NavigationService.Navigate(typeof(Views.ItemPage), (args.ChosenSuggestion as Item).Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(args.QueryText))
            {
                CurrentSearchProperties.InvokeSearchCanceledEvent();
                return;
            }

            UpdatePageHeader();
            UpdateView();
        }
        private void LanguageCodeChanged(SelectionChangedEventArgs args)
        {
            var selectedLanguage = args.AddedItems.FirstOrDefault() as Language;

            CurrentSearchProperties.Language = selectedLanguage as Language;
        }
        private void TagChanged(SelectionChangedEventArgs args)
        {
            var selectedTag = args.AddedItems.FirstOrDefault() as Tag;

            CurrentSearchProperties.Tag = selectedTag as Tag;
        }
        private void SetSortTypeFilter(string filter)
        {
            if (filter == "date")
                CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByCreationDate;
            else
                CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByReadingTime;
        }
        private void SetSortOrder(string order) => CurrentSearchProperties.OrderAscending = order == "asc";

        private int _previousItemTypeIndex;
        private void StartSearch()
        {
            IsSearchActive = true;
            _previousItemTypeIndex = CurrentSearchProperties.ItemTypeIndex;
            SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) => EndSearch(s, e);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }
        private void EndSearch(object sender, BackRequestedEventArgs e)
        {
            IsSearchActive = false;
            CurrentSearchProperties.ItemTypeIndex = _previousItemTypeIndex;

            SystemNavigationManager.GetForCurrentView().BackRequested -= (s, args) => EndSearch(s, args);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            if (e != null)
                e.Handled = true;

            CurrentSearchProperties.InvokeSearchCanceledEvent();

            if (!string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
            {
                CurrentSearchProperties.Query = string.Empty;
                UpdateView();
            }

            UpdatePageHeader();
        }

        private void UpdateView()
        {
            Items.Clear();

            var databaseItems = GetItemsForCurrentSearchProperties(limit: 24);

            foreach (var item in databaseItems)
                Items.Add(new ItemViewModel(item));

            GetMetadataForItems(Items);
        }
        private void GetMetadataForItems(IEnumerable<ItemViewModel> items)
        {
            foreach (var item in items)
            {
                if (item.Model.Language != null)
                {
                    var translatedLanguage = new Language(item.Model.Language);

                    if (!LanguageSuggestions.Contains(translatedLanguage))
                        LanguageSuggestions.AddSorted(translatedLanguage, sortAscending: true);
                }
                else
                {
                    if (!LanguageSuggestions.Contains(Language.Unknown))
                        LanguageSuggestions.AddSorted(Language.Unknown, sortAscending: true);
                }

                foreach (var tag in item.Model.Tags)
                    if (!TagSuggestions.Contains(tag))
                        TagSuggestions.AddSorted(tag, sortAscending: true);
            }

            if (LanguageSuggestions.Contains(Language.Unknown))
                LanguageSuggestions.Move(LanguageSuggestions.IndexOf(Language.Unknown), 0);
        }
        private List<Item> GetItemsForCurrentSearchProperties(int? offset = null, int? limit = null)
        {
            var items = App.Database.Table<Item>();

            if (string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
            {
                if (CurrentSearchProperties.ItemTypeIndex == 0)
                    items = items.Where(i => i.IsRead == false);
                else if (CurrentSearchProperties.ItemTypeIndex == 1)
                    items = items.Where(i => i.IsStarred == true);
                else if (CurrentSearchProperties.ItemTypeIndex == 2)
                    items = items.Where(i => i.IsRead == true);
            }

            if (!string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
                items = items.Where(i => i.Title.ToLower().Contains(CurrentSearchProperties.Query));

            if (CurrentSearchProperties.Language?.IsUnknown == false)
                items = items.Where(i => i.Language.Equals(CurrentSearchProperties.Language.wallabagLanguageCode));
            else if (CurrentSearchProperties.Language?.IsUnknown == true)
                items = items.Where(i => i.Language == null);

            if (CurrentSearchProperties.SortType == SearchProperties.SearchPropertiesSortType.ByReadingTime)
            {
                if (CurrentSearchProperties.OrderAscending == true)
                    items = items.OrderBy(i => i.EstimatedReadingTime);
                else
                    items = items.OrderByDescending(i => i.EstimatedReadingTime);
            }
            else
            {
                if (CurrentSearchProperties.OrderAscending == true)
                    items = items.OrderBy(i => i.CreationDate);
                else
                    items = items.OrderByDescending(i => i.CreationDate);
            }

            Items.MaxItems = items.Count();

            if (offset != null)
                items = items.Skip((int)offset);

            if (limit != null)
                items = items.Take((int)limit);

            var list = items.ToList();

            if (CurrentSearchProperties.Tag != null)
                list = list.Where(i => i.Tags.Contains(CurrentSearchProperties.Tag)).ToList();

            return list;
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            await TitleBarExtensions.ResetAsync();

            if (state.ContainsKey(nameof(CurrentSearchProperties)))
            {
                var stateValue = state[nameof(CurrentSearchProperties)] as string;
                CurrentSearchProperties.Replace(await Task.Run(() => JsonConvert.DeserializeObject<SearchProperties>(stateValue)));
            }

            UpdateView();

            if (SettingsService.Instance.SyncOnStartup)
                await SyncAsync();

            Messenger.Default.Register<NotificationMessage>(this, message =>
            {
                if (message.Notification.Equals("FetchFromDatabase"))
                    UpdateView();
            });
        }
        public override async Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            var serializedSearchProperties = await Task.Run(() => JsonConvert.SerializeObject(CurrentSearchProperties));
            pageState[nameof(CurrentSearchProperties)] = serializedSearchProperties;
        }
    }
}
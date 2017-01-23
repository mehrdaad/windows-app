using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using wallabag.Api;
using wallabag.Data.Common;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Common.Messages;
using wallabag.Data.Models;
using wallabag.Data.Services;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace wallabag.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        public IncrementalObservableCollection<ItemViewModel> Items { get; set; }

        public Visibility OfflineTaskVisibility => OfflineTaskCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        public int OfflineTaskCount => Database.ExecuteScalar<int>("select count(*) from OfflineTask");
        public bool IsSyncing { get; set; }

        public bool ItemsCountIsZero => Items.Count == 0;
        public bool IsSearchActive { get; set; } = false;
        public string PageHeader { get; set; } = GeneralHelper.LocalizedResource("SearchBox.PlaceholderText").ToUpper();

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
        public ICommand SetSortTypeFilterCommand { get; private set; }
        public ICommand SetSortOrderCommand { get; private set; }

        public ICommand SyncCommand { get; private set; }
        public ICommand AddCommand { get; private set; }
        public ICommand NavigateToSettingsPageCommand { get; private set; }

        public SearchProperties CurrentSearchProperties { get; private set; } = new SearchProperties();
        public ObservableCollection<Item> SearchQuerySuggestions { get; set; } = new ObservableCollection<Item>();
        public ObservableCollection<Language> LanguageSuggestions { get; set; } = new ObservableCollection<Language>();
        public ObservableCollection<Tag> TagSuggestions { get; set; } = new ObservableCollection<Tag>();
        public ICommand SearchQueryChangedCommand { get; private set; }
        public ICommand SearchQuerySubmittedCommand { get; private set; }
        public ICommand CloseSearchCommand { get; private set; }
        public ICommand LanguageCodeChangedCommand { get; private set; }
        public ICommand TagChangedCommand { get; private set; }
        public ICommand ResetFilterLanguageCommand { get; private set; }
        public ICommand ResetFilterTagCommand { get; private set; }
        public ICommand ResetFilterCommand { get; private set; }

        public MainViewModel()
        {
            LoggingService.WriteLine("Creating new instance of MainViewModel.");

            AddCommand = new RelayCommand(async () => await DialogService.ShowAsync(Dialogs.AddItemDialog));
            SyncCommand = new RelayCommand(async () => await SyncAsync());
            NavigateToSettingsPageCommand = new RelayCommand(() => Navigation.NavigateTo(Pages.SettingsPage));

            SetSortTypeFilterCommand = new RelayCommand<string>(filter => SetSortTypeFilter(filter));
            SetSortOrderCommand = new RelayCommand<string>(order => SetSortOrder(order));
            SearchQueryChangedCommand = new RelayCommand<AutoSuggestBoxTextChangedEventArgs>(async args => await SearchQueryChangedAsync(args));
            SearchQuerySubmittedCommand = new RelayCommand<AutoSuggestBoxQuerySubmittedEventArgs>(async args => await SearchQuerySubmittedAsync(args));
            CloseSearchCommand = new RelayCommand(() => EndSearchAsync(this, null));
            LanguageCodeChangedCommand = new RelayCommand<SelectionChangedEventArgs>(args => LanguageCodeChanged(args));
            TagChangedCommand = new RelayCommand<SelectionChangedEventArgs>(args => TagChanged(args));
            ResetFilterLanguageCommand = new RelayCommand(() => CurrentSearchProperties.Language = null);
            ResetFilterTagCommand = new RelayCommand(() => CurrentSearchProperties.Tag = null);
            ResetFilterCommand = new RelayCommand(() => CurrentSearchProperties.Reset());

            CurrentSearchProperties.SearchStarted += (s, e) => StartSearch();
            CurrentSearchProperties.PropertyChanged += async (s, e) =>
            {
                LoggingService.WriteLine($"The current search properties have been changed. PropertyName: {e.PropertyName}");

                if (e.PropertyName != nameof(CurrentSearchProperties.Query))
                    await ReloadViewAsync();

                RaisePropertyChanged(nameof(SortByCreationDate));
                RaisePropertyChanged(nameof(SortByReadingTime));
            };

            Items = new IncrementalObservableCollection<ItemViewModel>(async count => await LoadMoreItemsAsync(count));
            Items.CollectionChanged += (s, e) => RaisePropertyChanged(nameof(ItemsCountIsZero));

            OfflineTaskService.Tasks.CollectionChanged += async (s, e) =>
            {
                LoggingService.WriteLine($"The number of offline tasks changed. {e.NewItems?.Count} new items, {e.OldItems?.Count} old items.");

                RaisePropertyChanged(nameof(OfflineTaskCount));
                RaisePropertyChanged(nameof(OfflineTaskVisibility));

                if (e.NewItems != null && e.NewItems.Count > 0)
                    await ApplyUIChangesForOfflineTaskAsync(e.NewItems[0] as OfflineTask);
            };
        }

        private Task ApplyUIChangesForOfflineTaskAsync(OfflineTask task)
        {
            LoggingService.WriteLine("Executing UI changes for offline task.");
            LoggingService.WriteObject(task);

            var item = default(ItemViewModel);
            bool orderAscending = CurrentSearchProperties.OrderAscending ?? false;

            if (task.Action != OfflineTask.OfflineTaskAction.Delete)
            {
                item = ItemViewModel.FromId(task.ItemId);

                if (item == null)
                {
                    LoggingService.WriteLine("The item doesn't seem to be longer existing in the database. Existing.");
                    return Task.CompletedTask;
                }
            }

            return CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                LoggingService.WriteLine("Running dispatcher to apply the changes...");
                switch (task.Action)
                {
                    case OfflineTask.OfflineTaskAction.MarkAsRead:
                        if (CurrentSearchProperties.ItemTypeIndex == 2)
                            Items.AddSorted(item, sortAscending: orderAscending);
                        else
                            Items.Remove(item);
                        break;
                    case OfflineTask.OfflineTaskAction.UnmarkAsRead:
                        if (CurrentSearchProperties.ItemTypeIndex == 2)
                            Items.Remove(item);
                        else
                            Items.AddSorted(item, sortAscending: orderAscending);
                        break;
                    case OfflineTask.OfflineTaskAction.MarkAsStarred: break;
                    case OfflineTask.OfflineTaskAction.UnmarkAsStarred:
                        if (CurrentSearchProperties.ItemTypeIndex == 1)
                            Items.Remove(item);
                        break;
                    case OfflineTask.OfflineTaskAction.EditTags: break;
                    case OfflineTask.OfflineTaskAction.AddItem:
                        if (CurrentSearchProperties.ItemTypeIndex == 0)
                            Items.AddSorted(item, sortAscending: orderAscending);
                        break;
                    case OfflineTask.OfflineTaskAction.Delete:
                        Items.Remove(Items.Where(i => i.Model.Id.Equals(task.ItemId)).First());
                        break;
                }
            }).AsTask();
        }

        private async Task<List<ItemViewModel>> LoadMoreItemsAsync(uint count)
        {
            LoggingService.WriteLine("Loading more items from the database.");
            LoggingService.WriteLineIf(_incrementalLoadingIsBlocked, "Incremental loading is blocked.");
            if (_incrementalLoadingIsBlocked)
                return new List<ItemViewModel>();

            var result = new List<ItemViewModel>();

            LoggingService.WriteLine("Calling database for more items.");
            var database = await GetItemsForCurrentSearchPropertiesAsync(Items.Count, (int)count);

            LoggingService.WriteLine($"Adding {database.Count} items to current view.");
            foreach (var item in database)
                result.Add(new ItemViewModel(item));

            LoggingService.WriteLine("Fetching metadata for new items.");
            await GetMetadataForItemsAsync(result);

            return result;
        }

        private async Task SyncAsync()
        {
            LoggingService.WriteLine("Syncing with the server.");
            if (GeneralHelper.InternetConnectionIsAvailable == false)
            {
                LoggingService.WriteLine("No internet connection available.");
                return;
            }

            IsSyncing = true;
            LoggingService.WriteLine("Executing all offline tasks.");
            await OfflineTaskService.ExecuteAllAsync();
            int syncLimit = 24;

            LoggingService.WriteLine("Fetching items from the server.");
            var items = await Client.GetItemsAsync(
                dateOrder: WallabagClient.WallabagDateOrder.ByLastModificationDate,
                sortOrder: WallabagClient.WallabagSortOrder.Descending,
                itemsPerPage: syncLimit);

            LoggingService.WriteLineIf(items == null, "Fetching items failed.");

            if (items != null)
            {
                var itemList = new List<Item>();

                foreach (var item in items)
                    itemList.Add(item);

                LoggingService.WriteLine("Fetching items from the database to compare the new list with current items.");
                var databaseList = Database.Query<Item>($"SELECT Id FROM Item ORDER BY LastModificationDate DESC LIMIT 0,{syncLimit}", Array.Empty<object>());
                var deletedItems = databaseList.Except(itemList);

                LoggingService.WriteLine($"Number of deleted items: {deletedItems.Count()}");

                LoggingService.WriteLine("Updating the database.");
                Database.RunInTransaction(() =>
                {
                    foreach (var item in deletedItems)
                        Database.Delete(item);

                    Database.InsertOrReplaceAll(itemList);
                });

                if (Items.Count == 0 || databaseList[0].Equals(Items[0].Model) == false)
                    await ReloadViewAsync();

                Settings.General.LastSuccessfulSyncDateTime = DateTime.Now;
            }

            LoggingService.WriteLine("Fetching the tags from the server.");
            var tags = await Client.GetTagsAsync();

            LoggingService.WriteLineIf(tags == null, "Fetching tags failed.");

            if (tags != null)
            {
                LoggingService.WriteLine("Updating the database.");

                Database.RunInTransaction(() =>
                {
                    foreach (var tag in tags)
                        Database.InsertOrReplace((Tag)tag);
                });
            }

            IsSyncing = false;
            LoggingService.WriteLine("Syncing completed.");
        }

        internal void ItemClick(object sender, ItemClickEventArgs args)
        {
            var item = args.ClickedItem as ItemViewModel;

            LoggingService.WriteLine($"Clicked item: {item.Model.Id} ({item.Model.Title})");

            Navigation.NavigateTo(Pages.ItemPage, item.Model.Id);
        }

        private void UpdatePageHeader()
        {
            LoggingService.WriteLine("Updating page header.");
            LoggingService.WriteLine($"Old value: {PageHeader}");

            if (IsSearchActive)
                PageHeader = string.Format(GeneralHelper.LocalizedResource("SearchHeaderWithQuery").ToUpper(), "\"" + CurrentSearchProperties.Query + "\"");
            else
                PageHeader = GeneralHelper.LocalizedResource("SearchBox.PlaceholderText").ToUpper();

            LoggingService.WriteLine($"New value: {PageHeader}");
        }

        private async Task SearchQueryChangedAsync(AutoSuggestBoxTextChangedEventArgs args)
        {
            LoggingService.WriteLine($"Search query changed: {CurrentSearchProperties.Query}");

            if (string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
                return;

            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    LoggingService.WriteLine("Updating list of suggestions.");

                    var suggestions = Database.Query<Item>($"SELECT Id,Title FROM Item WHERE Title LIKE '%{CurrentSearchProperties.Query}%' LIMIT 5");
                    SearchQuerySuggestions.Replace(suggestions);
                }
            });
        }
        private async Task SearchQuerySubmittedAsync(AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            LoggingService.WriteLine($"Search query was submitted: {args.QueryText}");
            LoggingService.WriteLineIf(args.ChosenSuggestion == null, "No suggestion was chosen.");

            if (args.ChosenSuggestion != null)
            {
                var item = args.ChosenSuggestion as Item;

                LoggingService.WriteLine($"Chosen suggestion: {item.Id} ({item.Title})");
                Navigation.NavigateTo(Pages.ItemPage, item.Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(args.QueryText))
            {
                LoggingService.WriteLine("Cancelling the search, because the query text is empty.");
                CurrentSearchProperties.InvokeSearchCanceledEvent();
                return;
            }

            UpdatePageHeader();
            await ReloadViewAsync();
        }
        private void LanguageCodeChanged(SelectionChangedEventArgs args)
        {
            var selectedLanguage = args.AddedItems.FirstOrDefault() as Language;
            CurrentSearchProperties.Language = selectedLanguage;

            LoggingService.WriteLine($"Language code changed to {selectedLanguage?.LanguageCode} ({selectedLanguage?.InternalLanguageCode}).");
        }
        private void TagChanged(SelectionChangedEventArgs args)
        {
            var selectedTag = args.AddedItems.FirstOrDefault() as Tag;
            CurrentSearchProperties.Tag = selectedTag;

            LoggingService.WriteLine($"Language code changed to {selectedTag?.Label}.");
        }
        private void SetSortTypeFilter(string filter)
        {
            if (filter == "date")
                CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByCreationDate;
            else
                CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByReadingTime;

            LoggingService.WriteLine($"Sort type changed to {CurrentSearchProperties.SortType}.");
        }
        private void SetSortOrder(string order) => CurrentSearchProperties.OrderAscending = order == "asc";

        private int _previousItemTypeIndex;
        private bool _incrementalLoadingIsBlocked;

        private void StartSearch()
        {
            LoggingService.WriteLine("Starting the search process.");

            IsSearchActive = true;
            _previousItemTypeIndex = CurrentSearchProperties.ItemTypeIndex;
            SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) => EndSearchAsync(s, e);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }
        private async void EndSearchAsync(object sender, BackRequestedEventArgs e)
        {
            LoggingService.WriteLine("Ending the search process.");

            IsSearchActive = false;
            CurrentSearchProperties.ItemTypeIndex = _previousItemTypeIndex;

            SystemNavigationManager.GetForCurrentView().BackRequested -= (s, args) => EndSearchAsync(s, args);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            if (e != null)
                e.Handled = true;

            CurrentSearchProperties.InvokeSearchCanceledEvent();

            if (!string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
            {
                CurrentSearchProperties.Query = string.Empty;
                await ReloadViewAsync();
            }

            UpdatePageHeader();
        }

        private async Task ReloadViewAsync()
        {
            LoggingService.WriteLine("Reloading the view.");

            var databaseItems = await GetItemsForCurrentSearchPropertiesAsync();
            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
             {
                 Items.Clear();

                 foreach (var item in databaseItems)
                     Items.Add(new ItemViewModel(item));
             });
            await GetMetadataForItemsAsync(Items);
        }
        private Windows.Foundation.IAsyncAction GetMetadataForItemsAsync(IEnumerable<ItemViewModel> items)
        {
            LoggingService.WriteLine($"Fetching metadata for {items.Count()} items.");

            return CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                foreach (var item in items)
                {
                    if (item.Model.Language != null)
                    {
                        var translatedLanguage = new Language(item.Model.Language);

                        if (!LanguageSuggestions.Contains(translatedLanguage))
                        {
                            LanguageSuggestions.AddSorted(translatedLanguage, sortAscending: true);
                            LoggingService.WriteLine($"Language code {translatedLanguage.LanguageCode} ({translatedLanguage.InternalLanguageCode}) is not in the list. Added.");
                        }
                    }
                    else
                    {
                        if (!LanguageSuggestions.Contains(Language.Unknown))
                        {
                            LanguageSuggestions.AddSorted(Language.Unknown, sortAscending: true);
                            LoggingService.WriteLine("Added Language.Unknown to the list.");
                        }
                    }

                    foreach (var tag in item.Model.Tags)
                    {
                        if (!TagSuggestions.Contains(tag))
                        {
                            TagSuggestions.AddSorted(tag, sortAscending: true);
                            LoggingService.WriteLine($"Tag {tag.Label} is not in the list. Added.");
                        }
                    }
                }

                if (LanguageSuggestions.Contains(Language.Unknown))
                    LanguageSuggestions.Move(LanguageSuggestions.IndexOf(Language.Unknown), 0);
            });
        }
        private Task<List<Item>> GetItemsForCurrentSearchPropertiesAsync(int offset = 0, int limit = 24)
        {
            LoggingService.WriteLine($"Getting items for current search properties. Offset {offset}, Limit {limit}.");

            return Task.Factory.StartNew(() =>
            {
                string sqlPropertyString = string.Join(",", typeof(Item).GetProperties().Select(p => p.Name)).Replace($"{nameof(Item.Content)},", string.Empty);

                string queryStart = $"SELECT {sqlPropertyString} FROM Item";
                var queryParts = new List<string>();
                var queryParameters = new List<object>();

                if (string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
                {
                    if (CurrentSearchProperties.ItemTypeIndex == 0)
                    {
                        queryParts.Add("IsRead=?");
                        queryParameters.Add(0);
                    }
                    else if (CurrentSearchProperties.ItemTypeIndex == 1)
                    {
                        queryParts.Add("IsStarred=?");
                        queryParameters.Add(1);
                    }
                    else if (CurrentSearchProperties.ItemTypeIndex == 2)
                    {
                        queryParts.Add("IsRead=?");
                        queryParameters.Add(1);
                    }
                }

                if (!string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
                    queryParts.Add($"Title LIKE '%{CurrentSearchProperties.Query}%'");

                if (CurrentSearchProperties.Language?.IsUnknown == false)
                {
                    queryParts.Add("Language=?");
                    queryParameters.Add(CurrentSearchProperties.Language.InternalLanguageCode);
                }
                else if (CurrentSearchProperties.Language?.IsUnknown == true)
                    queryParts.Add("Language IS NULL");

                if (CurrentSearchProperties.Tag != null)
                    queryParts.Add($"Tags LIKE '%{CurrentSearchProperties.Tag.Label}%'");

                string query = BuildSQLQuery(queryStart, queryParts);

                if (CurrentSearchProperties.SortType == SearchProperties.SearchPropertiesSortType.ByReadingTime)
                {
                    if (CurrentSearchProperties.OrderAscending == true)
                        query += " ORDER BY EstimatedReadingTime ASC";
                    else
                        query += " ORDER BY EstimatedReadingTime DESC";
                }
                else
                {
                    if (CurrentSearchProperties.OrderAscending == true)
                        query += " ORDER BY CreationDate ASC";
                    else
                        query += " ORDER BY CreationDate DESC";
                }

                Items.MaxItems = Database.ExecuteScalar<int>(query.Replace(queryStart, "SELECT count(*) FROM Item"), queryParameters.ToArray());
                LoggingService.WriteLine($"Maximum number of items: {Items.MaxItems}");

                query += " LIMIT ?,?";
                queryParameters.Add(offset);
                queryParameters.Add(limit);

                LoggingService.WriteLine($"SQL query: {query}");
                LoggingService.WriteLine($"SQL parameters: {string.Join(";", queryParameters)}");

                return Database.Query<Item>(query, queryParameters.ToArray());
            });
        }

        private string BuildSQLQuery(string start, List<string> queries)
        {
            LoggingService.WriteLine($"Building the SQL query. Start: {start}");

            string result = start;
            if (start.EndsWith(" ") == false)
                result += " ";

            foreach (string item in queries)
            {
                int queryIndex = queries.IndexOf(item);
                if (queryIndex == 0)
                    result += "WHERE " + item;
                else
                    result += "AND " + item;
                result += " ";
            }

            LoggingService.WriteLine($"Final query: {result}");
            return result;
        }

        public override async Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state)
        {
            _incrementalLoadingIsBlocked = true;

            if (state.ContainsKey(nameof(CurrentSearchProperties)))
            {
                LoggingService.WriteLine("Restoring search properties from page state.");
                string stateValue = state[nameof(CurrentSearchProperties)] as string;
                CurrentSearchProperties.Replace(await Task.Run(() => JsonConvert.DeserializeObject<SearchProperties>(stateValue)));
            }

            await ReloadViewAsync();
            _incrementalLoadingIsBlocked = false;

            if (Settings.General.SyncOnStartup)
                await SyncAsync();

            Messenger.Default.Register<UpdateItemMessage>(this, message =>
            {
                var viewModel = ItemViewModel.FromId(message.ItemId);

                LoggingService.WriteLine($"Updating item with ID {message.ItemId}.");
                LoggingService.WriteLineIf(viewModel == null, "Item does not exist in the database!", LoggingCategory.Warning);

                if (viewModel != null && Items.Contains(viewModel))
                {
                    Items.Remove(viewModel); // This is only working because the app is just comparing the ID's
                    Items.AddSorted(viewModel, sortAscending: CurrentSearchProperties.OrderAscending == true);
                }
            });
        }
        public override async Task OnNavigatedFromAsync(IDictionary<string, object> pageState)
        {
            string serializedSearchProperties = await Task.Run(() => JsonConvert.SerializeObject(CurrentSearchProperties));
            pageState[nameof(CurrentSearchProperties)] = serializedSearchProperties;

            Messenger.Default.Unregister(this);
        }
    }
}
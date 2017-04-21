using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using PropertyChanged;
using SQLite.Net;
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
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using wallabag.Data.Services.OfflineTaskService;

namespace wallabag.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        private readonly ILoggingService _loggingService;
        private readonly IOfflineTaskService _offlineTaskService;
        private readonly INavigationService _navigationService;
        private readonly IWallabagClient _client;
        private readonly IPlatformSpecific _device;
        private readonly SQLiteConnection _database;

        private int _previousItemTypeIndex;
        public ObservableCollection<ItemViewModel> Items { get; set; }

        // General
        public int OfflineTaskCount => _database.ExecuteScalar<int>("select count(*) from OfflineTask");
        public bool OfflineTaskIndicatorIsVisible => OfflineTaskCount > 0;
        public bool ItemsCountIsZero => Items.Count == 0;
        public bool IsSyncing { get; set; }
        public string PageHeader { get; set; }

        public ICommand SyncCommand { get; private set; }
        public ICommand AddCommand { get; private set; }
        public ICommand NavigateToSettingsPageCommand { get; private set; }
        public ICommand ItemClickCommand { get; private set; }

        // Search & filter
        public bool IsSearchActive { get; set; } = false;
        public SearchProperties CurrentSearchProperties { get; private set; } = new SearchProperties();
        public ObservableCollection<Item> ItemSuggestions { get; set; } = new ObservableCollection<Item>();
        public ObservableCollection<Language> LanguageSuggestions { get; set; } = new ObservableCollection<Language>();
        public ObservableCollection<Tag> TagSuggestions { get; set; } = new ObservableCollection<Tag>();

        public ICommand SearchQuerySubmittedCommand { get; private set; }
        public ICommand ResetLanguageFilterCommand { get; private set; }
        public ICommand ResetTagFilterCommand { get; private set; }
        public ICommand ResetFilterCommand { get; private set; }
        public ICommand EndSearchCommand { get; private set; }

        // Sorting
        public bool SortByCreationDate
        {
            get => CurrentSearchProperties.SortType == SearchProperties.SearchPropertiesSortType.ByCreationDate;
            set => CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByCreationDate;
        }
        public bool SortByReadingTime
        {
            get => CurrentSearchProperties.SortType == SearchProperties.SearchPropertiesSortType.ByReadingTime;
            set => CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByReadingTime;
        }
        public ICommand SetSortTypeCommand { get; private set; }
        public ICommand SetSortOrderCommand { get; private set; }

        public MainViewModel(
            ILoggingService logging,
            IOfflineTaskService offlineTaskService,
            INavigationService navigation,
            IWallabagClient client,
            IPlatformSpecific platform,
            SQLiteConnection database)
        {
            _loggingService = logging;
            _offlineTaskService = offlineTaskService;
            _navigationService = navigation;
            _client = client;
            _device = platform;
            _database = database;

            _loggingService.WriteLine("Creating new instance of MainViewModel.");

            AddCommand = new RelayCommand(() => _navigationService.Navigate(Navigation.Pages.AddItemPage));
            SyncCommand = new RelayCommand(async () => await SyncAsync());
            NavigateToSettingsPageCommand = new RelayCommand(() => _navigationService.Navigate(Navigation.Pages.SettingsPage));
            ItemClickCommand = new RelayCommand<ItemViewModel>(item =>
            {
                _loggingService.WriteLine($"Clicked item: {item.Model.Id} ({item.Model.Title})");
                _navigationService.Navigate(Navigation.Pages.ItemPage, item.Model.Id);
            });

            SearchQuerySubmittedCommand = new RelayCommand(async () => await SearchQuerySubmittedAsync());
            ResetLanguageFilterCommand = new RelayCommand(() => CurrentSearchProperties.Language = null);
            ResetTagFilterCommand = new RelayCommand(() => CurrentSearchProperties.Tag = null);
            ResetFilterCommand = new RelayCommand(() => CurrentSearchProperties.Reset());
            EndSearchCommand = new RelayCommand(async () => await EndSearchAsync());
            SetSortTypeCommand = new RelayCommand<string>(filter => SetSortTypeFilter(filter));
            SetSortOrderCommand = new RelayCommand<string>(order => SetSortOrder(order));

            CurrentSearchProperties.SearchStarted += (s, e) => StartSearch();
            CurrentSearchProperties.PropertyChanged += async (s, e) =>
            {
                _loggingService.WriteLine($"The current search properties have been changed. PropertyName: {e.PropertyName}");

                if (e.PropertyName != nameof(CurrentSearchProperties.Query))
                    await ReloadViewAsync();

                RaisePropertyChanged(nameof(SortByCreationDate));
                RaisePropertyChanged(nameof(SortByReadingTime));
            };

            Items = new ObservableCollection<ItemViewModel>();
            Items.CollectionChanged += (s, e) => RaisePropertyChanged(nameof(ItemsCountIsZero));

            _offlineTaskService.TaskAdded += async (s, e) =>
            {
                _loggingService.WriteLine($"A new OfflineTask was added. ID: {e.Task.Id}");

                await _device.RunOnUIThreadAsync(() =>
                {
                    if (e.PlaceholderItemId >= 0)
                    {
                        var placeholder = ItemViewModel.FromId(
                            e.Task.Id,
                            _loggingService,
                            _database,
                            _offlineTaskService,
                            _navigationService,
                            _device);

                        if (placeholder != null)
                            Items.AddSorted(placeholder, sortAscending: CurrentSearchProperties.OrderAscending == true);
                    }

                    RaisePropertyChanged(nameof(OfflineTaskCount));
                    RaisePropertyChanged(nameof(OfflineTaskIndicatorIsVisible));
                });
            };
            _offlineTaskService.TaskExecuted += async (s, e) =>
            {
                _loggingService.WriteLine($"An OfflineTask was executed. ID: {e.Task.Id}");

                await _device.RunOnUIThreadAsync(async () =>
                {
                    RaisePropertyChanged(nameof(OfflineTaskCount));
                    RaisePropertyChanged(nameof(OfflineTaskIndicatorIsVisible));
                    await ApplyUIChangesForOfflineTaskAsync(e.Task);
                });
            };
        }

        public override async Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state, NavigationMode mode)
        {
            if (state.ContainsKey(nameof(CurrentSearchProperties)))
            {
                _loggingService.WriteLine("Restoring search properties from page state.");
                string stateValue = state[nameof(CurrentSearchProperties)] as string;
                CurrentSearchProperties.Replace(await Task.Run(() => JsonConvert.DeserializeObject<SearchProperties>(stateValue)));
            }

            await ReloadViewAsync();

            if (Settings.General.SyncOnStartup)
                await SyncAsync();

            Messenger.Default.Register<UpdateItemMessage>(this, message =>
            {
                var viewModel = ItemViewModel.FromId(
                    message.ItemId,
                    _loggingService,
                    _database,
                    _offlineTaskService,
                    _navigationService,
                    _device);

                _loggingService.WriteLine($"Updating item with ID {message.ItemId}.");
                _loggingService.WriteLineIf(viewModel == null, "Item does not exist in the database!", LoggingCategory.Warning);

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

        private async Task SyncAsync()
        {
            _loggingService.WriteLine("Syncing with the server.");
            if (_device.InternetConnectionIsAvailable == false)
            {
                _loggingService.WriteLine("No internet connection available.");
                return;
            }

            IsSyncing = true;
            _loggingService.WriteLine("Executing all offline tasks.");
            await _offlineTaskService.ExecuteAllAsync();
            int syncLimit = 24;

            _loggingService.WriteLine("Fetching items from the server.");
            var items = await _client.GetItemsAsync(
                dateOrder: WallabagClient.WallabagDateOrder.ByLastModificationDate,
                sortOrder: WallabagClient.WallabagSortOrder.Descending,
                itemsPerPage: syncLimit);

            _loggingService.WriteLineIf(items == null, "Fetching items failed.");

            if (items != null)
            {
                var itemList = new List<Item>();

                foreach (var item in items)
                    itemList.Add(item);

                _loggingService.WriteLine("Fetching items from the database to compare the new list with current items.");
                var databaseList = _database.Query<Item>($"SELECT Id FROM Item ORDER BY LastModificationDate DESC LIMIT 0,{syncLimit}", new object[0]);
                var deletedItems = databaseList.Except(itemList);

                _loggingService.WriteLine($"Number of deleted items: {deletedItems.Count()}");

                _loggingService.WriteLine("Updating the database.");
                _database.RunInTransaction(() =>
                {
                    foreach (var item in deletedItems)
                        _database.Delete(item);

                    _database.InsertOrReplaceAll(itemList);
                });

                if (Items.Count == 0 || databaseList[0].Equals(Items[0].Model) == false)
                    await ReloadViewAsync();

                Settings.General.LastSuccessfulSyncDateTime = DateTime.Now;
            }

            _loggingService.WriteLine("Fetching the tags from the server.");
            var tags = await _client.GetTagsAsync();

            _loggingService.WriteLineIf(tags == null, "Fetching tags failed.");

            if (tags != null)
            {
                _loggingService.WriteLine("Updating the database.");

                _database.RunInTransaction(() =>
                {
                    foreach (var tag in tags)
                        _database.InsertOrReplace((Tag)tag);
                });
            }

            IsSyncing = false;
            _loggingService.WriteLine("Syncing completed.");
        }
        private async Task ReloadViewAsync()
        {
            _loggingService.WriteLine("Reloading the view.");

            var databaseItems = await GetItemsForCurrentSearchPropertiesAsync();
            await _device.RunOnUIThreadAsync(() =>
            {
                Items.Clear();

                foreach (var item in databaseItems)
                    Items.Add(new ItemViewModel(item, _offlineTaskService, _navigationService, _loggingService, _device, _database));
            });
            await GetMetadataForItemsAsync(Items);
        }
        private Task ApplyUIChangesForOfflineTaskAsync(OfflineTask task, int placeholderItemId = -1)
        {
            _loggingService.WriteLine("Executing UI changes for offline task.");
            _loggingService.WriteObject(task);

            var item = default(ItemViewModel);
            bool orderAscending = CurrentSearchProperties.OrderAscending ?? false;

            if (task.Action != OfflineTask.OfflineTaskAction.Delete)
            {
                item = ItemViewModel.FromId(
                    task.ItemId,
                    _loggingService,
                    _database,
                    _offlineTaskService,
                    _navigationService,
                    _device);

                if (item == null)
                {
                    _loggingService.WriteLine("The item doesn't seem to be longer existing in the database. Existing.");
                    return Task.FromResult(true);
                }
            }

            return _device.RunOnUIThreadAsync(() =>
            {
                _loggingService.WriteLine("Running dispatcher to apply the changes...");
                switch (task.Action)
                {
                    case OfflineTask.OfflineTaskAction.MarkAsRead:
                        if (CurrentSearchProperties.ItemTypeIndex == 2)
                            Items.AddSorted(item, sortAscending: orderAscending);
                        else if (CurrentSearchProperties.ItemTypeIndex == 0)
                            Items.Remove(item);
                        break;
                    case OfflineTask.OfflineTaskAction.UnmarkAsRead:
                        if (CurrentSearchProperties.ItemTypeIndex == 2)
                            Items.Remove(item);
                        else if (CurrentSearchProperties.ItemTypeIndex == 0)
                            Items.AddSorted(item, sortAscending: orderAscending);
                        break;
                    case OfflineTask.OfflineTaskAction.MarkAsStarred: break;
                    case OfflineTask.OfflineTaskAction.UnmarkAsStarred:
                        if (CurrentSearchProperties.ItemTypeIndex == 1)
                            Items.Remove(item);
                        break;
                    case OfflineTask.OfflineTaskAction.EditTags:
                        Items.Remove(item);
                        Items.AddSorted(item, sortAscending: orderAscending);
                        break;
                    case OfflineTask.OfflineTaskAction.AddItem:
                        if (CurrentSearchProperties.ItemTypeIndex == 0)
                        {
                            var placeholder = ItemViewModel.FromId(placeholderItemId,
                                _loggingService,
                                _database,
                                _offlineTaskService,
                                _navigationService,
                                _device);
                            Items.AddSorted(placeholder, sortAscending: orderAscending);
                        }
                        break;
                    case OfflineTask.OfflineTaskAction.Delete:
                        Items.Remove(Items.Where(i => i.Model.Id.Equals(task.ItemId)).First());
                        break;
                }
            });
        }
        private async Task<List<ItemViewModel>> LoadMoreItemsAsync(int count)
        {
            _loggingService.WriteLine("Loading more items from the database.");

            var result = new List<ItemViewModel>();

            _loggingService.WriteLine("Calling database for more items.");
            var database = await GetItemsForCurrentSearchPropertiesAsync(Items.Count, (int)count);

            _loggingService.WriteLine($"Adding {database.Count} items to current view.");
            foreach (var item in database)
                result.Add(new ItemViewModel(item, _offlineTaskService, _navigationService, _loggingService, _device, _database));

            _loggingService.WriteLine("Fetching metadata for new items.");
            await GetMetadataForItemsAsync(result);

            return result;
        }
        private Task GetMetadataForItemsAsync(IEnumerable<ItemViewModel> items)
        {
            _loggingService.WriteLine($"Fetching metadata for {items?.Count()} items.");

            return _device.RunOnUIThreadAsync(() =>
            {
                foreach (var item in items)
                {
                    if (item.Model.Language != null)
                    {
                        var translatedLanguage = new Language(item.Model.Language);

                        if (!LanguageSuggestions.Contains(translatedLanguage))
                        {
                            LanguageSuggestions.AddSorted(translatedLanguage, sortAscending: true);
                            _loggingService.WriteLine($"Language code {translatedLanguage.LanguageCode} ({translatedLanguage.InternalLanguageCode}) is not in the list. Added.");
                        }
                    }
                    else
                    {
                        if (!LanguageSuggestions.Contains(Language.Unknown))
                        {
                            LanguageSuggestions.AddSorted(Language.Unknown, sortAscending: true);
                            _loggingService.WriteLine("Added Language.Unknown to the list.");
                        }
                    }

                    foreach (var tag in item.Model.Tags)
                    {
                        if (!TagSuggestions.Contains(tag))
                        {
                            TagSuggestions.AddSorted(tag, sortAscending: true);
                            _loggingService.WriteLine($"Tag {tag.Label} is not in the list. Added.");
                        }
                    }
                }

                if (LanguageSuggestions.Contains(Language.Unknown))
                    LanguageSuggestions.Move(LanguageSuggestions.IndexOf(Language.Unknown), 0);
            });
        }

        private Task<List<Item>> GetItemsForCurrentSearchPropertiesAsync(int offset = 0, int limit = 24)
        {
            _loggingService.WriteLine($"Getting items for current search properties. Offset {offset}, Limit {limit}.");

            return Task.Factory.StartNew(() =>
            {
                string sqlPropertyString = string.Join(",", typeof(Item).GetRuntimeProperties().Select(p => p.Name)).Replace($"{nameof(Item.Content)},", string.Empty);

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

                //query += " LIMIT ?,?";
                //queryParameters.Add(offset);
                //queryParameters.Add(limit);

                _loggingService.WriteLine($"SQL query: {query}");
                _loggingService.WriteLine($"SQL parameters: {string.Join(";", queryParameters)}");

                return _database.Query<Item>(query, queryParameters.ToArray());
            });
        }
        private async Task SearchQueryChangedAsync()
        {
            _loggingService.WriteLine($"Search query changed: {CurrentSearchProperties.Query}");

            if (string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
                return;

            await _device.RunOnUIThreadAsync(() =>
            {
                _loggingService.WriteLine("Updating list of suggestions.");

                var suggestions = _database.Query<Item>($"SELECT Id,Title FROM Item WHERE Title LIKE '%{CurrentSearchProperties.Query}%' LIMIT 5");
                ItemSuggestions.Replace(suggestions);
            });
        }
        private async Task SearchQuerySubmittedAsync(Item chosenSuggestion = null)
        {
            _loggingService.WriteLine($"Search query was submitted: {CurrentSearchProperties.Query}");
            _loggingService.WriteLineIf(chosenSuggestion == null, "No suggestion was chosen.");

            if (chosenSuggestion != null)
            {
                var item = chosenSuggestion as Item;

                _loggingService.WriteLine($"Chosen suggestion: {item.Id} ({item.Title})");
                _navigationService.Navigate(Navigation.Pages.ItemPage, item.Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
            {
                _loggingService.WriteLine("Cancelling the search, because the query text is empty.");
                CurrentSearchProperties.InvokeSearchCanceledEvent();
                return;
            }

            UpdatePageHeader();
            await ReloadViewAsync();
        }
        private void SetSortTypeFilter(string filter)
        {
            if (filter == "date")
                CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByCreationDate;
            else
                CurrentSearchProperties.SortType = SearchProperties.SearchPropertiesSortType.ByReadingTime;

            _loggingService.WriteLine($"Sort type changed to {CurrentSearchProperties.SortType}.");
        }
        private void SetSortOrder(string order) => CurrentSearchProperties.OrderAscending = order == "asc";

        private void StartSearch()
        {
            _loggingService.WriteLine("Starting the search process.");

            IsSearchActive = true;
            _previousItemTypeIndex = CurrentSearchProperties.ItemTypeIndex;
        }
        private async Task EndSearchAsync()
        {
            _loggingService.WriteLine("Ending the search process.");

            IsSearchActive = false;
            CurrentSearchProperties.ItemTypeIndex = _previousItemTypeIndex;

            CurrentSearchProperties.InvokeSearchCanceledEvent();

            if (!string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
            {
                CurrentSearchProperties.Query = string.Empty;
                await ReloadViewAsync();
            }

            UpdatePageHeader();
        }

        private void UpdatePageHeader()
        {
            _loggingService.WriteLine("Updating page header.");
            _loggingService.WriteLine($"Old value: {PageHeader}");

            if (IsSearchActive)
                PageHeader = string.Format(_device.GetLocalizedResource("SearchHeaderWithQuery").ToUpper(), "\"" + CurrentSearchProperties.Query + "\"");
            else
                PageHeader = _device.GetLocalizedResource("SearchBox.PlaceholderText").ToUpper();

            _loggingService.WriteLine($"New value: {PageHeader}");
        }
        private string BuildSQLQuery(string start, List<string> queries)
        {
            _loggingService.WriteLine($"Building the SQL query. Start: {start}");

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

            _loggingService.WriteLine($"Final query: {result}");
            return result;
        }
    }
}
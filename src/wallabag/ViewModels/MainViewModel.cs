using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Common;
using wallabag.Common.Helpers;
using wallabag.Common.Messages;
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

        public Visibility OfflineTaskVisibility => OfflineTaskCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        public int OfflineTaskCount => App.Database.ExecuteScalar<int>("select count(*) from OfflineTask");
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
        public DelegateCommand<string> SetSortTypeFilterCommand { get; private set; }
        public DelegateCommand<string> SetSortOrderCommand { get; private set; }

        public DelegateCommand SyncCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand NavigateToSettingsPageCommand { get; private set; }

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
            SearchQueryChangedCommand = new DelegateCommand<AutoSuggestBoxTextChangedEventArgs>(async args => await SearchQueryChangedAsync(args));
            SearchQuerySubmittedCommand = new DelegateCommand<AutoSuggestBoxQuerySubmittedEventArgs>(async args => await SearchQuerySubmittedAsync(args));
            CloseSearchCommand = new DelegateCommand(() => EndSearchAsync(this, null));
            LanguageCodeChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(args => LanguageCodeChanged(args));
            TagChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(args => TagChanged(args));
            ResetFilterLanguageCommand = new DelegateCommand(() => CurrentSearchProperties.Language = null);
            ResetFilterTagCommand = new DelegateCommand(() => CurrentSearchProperties.Tag = null);
            ResetFilterCommand = new DelegateCommand(() => CurrentSearchProperties.Reset());

            CurrentSearchProperties.SearchStarted += (s, e) => StartSearch();
            CurrentSearchProperties.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName != nameof(CurrentSearchProperties.Query))
                    await ReloadViewAsync();

                RaisePropertyChanged(nameof(SortByCreationDate));
                RaisePropertyChanged(nameof(SortByReadingTime));
            };

            Items = new IncrementalObservableCollection<ItemViewModel>(async count => await LoadMoreItemsAsync(count));
            Items.CollectionChanged += (s, e) => RaisePropertyChanged(nameof(ItemsCountIsZero));

            OfflineTaskService.Tasks.CollectionChanged += async (s, e) =>
            {
                if (e.NewItems != null && e.NewItems.Count > 0)
                    await ApplyUIChangesForOfflineTaskAsync(e.NewItems[0] as OfflineTask);

                RaisePropertyChangesForOfflineTaskProperties();
            };
        }

        private Task ApplyUIChangesForOfflineTaskAsync(OfflineTask task)
        {
            var item = default(ItemViewModel);
            bool orderAscending = CurrentSearchProperties.OrderAscending ?? false;

            if (task.Action != OfflineTask.OfflineTaskAction.Delete)
            {
                item = ItemViewModel.FromId(task.ItemId);

                if (item == null)
                    return Task.CompletedTask;
            }

            return CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
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
        private void RaisePropertyChangesForOfflineTaskProperties()
        {
            RaisePropertyChanged(nameof(OfflineTaskCount));
            RaisePropertyChanged(nameof(OfflineTaskVisibility));
        }

        private async Task<List<ItemViewModel>> LoadMoreItemsAsync(uint count)
        {
            if (_incrementalLoadingIsBlocked)
                return new List<ItemViewModel>();

            var result = new List<ItemViewModel>();

            var database = await GetItemsForCurrentSearchPropertiesAsync(Items.Count, (int)count);

            foreach (var item in database)
                result.Add(new ItemViewModel(item));

            await GetMetadataForItemsAsync(result);

            return result;
        }

        private async Task SyncAsync()
        {
            if (GeneralHelper.InternetConnectionIsAvailable == false)
                return;

            IsSyncing = true;
            await OfflineTaskService.ExecuteAllAsync().ContinueWith(x => RaisePropertyChangesForOfflineTaskProperties());
            int syncLimit = 24;

            var items = await App.Client.GetItemsAsync(
                dateOrder: Api.WallabagClient.WallabagDateOrder.ByLastModificationDate,
                sortOrder: Api.WallabagClient.WallabagSortOrder.Descending,
                itemsPerPage: syncLimit);

            if (items != null)
            {
                var itemList = new List<Item>();

                foreach (var item in items)
                    itemList.Add(item);

                var databaseList = App.Database.Query<Item>($"SELECT Id FROM Item ORDER BY LastModificationDate DESC LIMIT 0,{syncLimit}", Array.Empty<object>());
                var deletedItems = databaseList.Except(itemList);

                App.Database.RunInTransaction(() =>
                {
                    foreach (var item in deletedItems)
                        App.Database.Delete(item);

                    App.Database.InsertOrReplaceAll(itemList);
                });

                if (Items.Count == 0 || databaseList[0].Equals(Items[0].Model) == false)
                    await ReloadViewAsync();

                SettingsService.Instance.LastSuccessfulSyncDateTime = DateTime.Now;
            }

            var tags = await App.Client.GetTagsAsync();

            if (tags != null)
            {
                App.Database.RunInTransaction(() =>
                {
                    foreach (var tag in tags)
                        App.Database.InsertOrReplace((Tag)tag);
                });
            }

            RaisePropertyChangesForOfflineTaskProperties();
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
                PageHeader = string.Format(GeneralHelper.LocalizedResource("SearchHeaderWithQuery").ToUpper(), "\"" + CurrentSearchProperties.Query + "\"");
            else
                PageHeader = GeneralHelper.LocalizedResource("SearchBox.PlaceholderText").ToUpper();
        }

        private async Task SearchQueryChangedAsync(AutoSuggestBoxTextChangedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(CurrentSearchProperties.Query))
                return;

            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    var suggestions = App.Database.Query<Item>($"SELECT Id,Title FROM Item WHERE Title LIKE '%{CurrentSearchProperties.Query}%' LIMIT 5");
                    SearchQuerySuggestions.Replace(suggestions);
                }
            });
        }
        private async Task SearchQuerySubmittedAsync(AutoSuggestBoxQuerySubmittedEventArgs args)
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
            await ReloadViewAsync();
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
        private bool _incrementalLoadingIsBlocked;

        private void StartSearch()
        {
            IsSearchActive = true;
            _previousItemTypeIndex = CurrentSearchProperties.ItemTypeIndex;
            SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) => EndSearchAsync(s, e);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }
        private async void EndSearchAsync(object sender, BackRequestedEventArgs e)
        {
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
            return CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
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
            });
        }
        private Task<List<Item>> GetItemsForCurrentSearchPropertiesAsync(int offset = 0, int limit = 24)
        {
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

                Items.MaxItems = App.Database.ExecuteScalar<int>(query.Replace(queryStart, "SELECT count(*) FROM Item"), queryParameters.ToArray());

                query += " LIMIT ?,?";
                queryParameters.Add(offset);
                queryParameters.Add(limit);

                return App.Database.Query<Item>(query, queryParameters.ToArray());
            });
        }

        private string BuildSQLQuery(string start, List<string> queries)
        {
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

            return result;
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            _incrementalLoadingIsBlocked = true;
            await TitleBarHelper.ResetAsync();

            if (mode != NavigationMode.Back && mode != NavigationMode.Forward)
            {
                if (state.ContainsKey(nameof(CurrentSearchProperties)))
                {
                    string stateValue = state[nameof(CurrentSearchProperties)] as string;
                    CurrentSearchProperties.Replace(await Task.Run(() => JsonConvert.DeserializeObject<SearchProperties>(stateValue)));
                }

                await ReloadViewAsync();
                _incrementalLoadingIsBlocked = false;

                if (SettingsService.Instance.SyncOnStartup)
                    await SyncAsync();
            }

            Messenger.Default.Register<UpdateItemMessage>(this, message =>
            {
                var viewModel = ItemViewModel.FromId(message.ItemId);

                if (viewModel != null && Items.Contains(viewModel))
                {
                    Items.Remove(viewModel); // This is only working because the app is just comparing the ID's
                    Items.AddSorted(viewModel, sortAscending: CurrentSearchProperties.OrderAscending == true);
                }
            });
        }
        public override async Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            string serializedSearchProperties = await Task.Run(() => JsonConvert.SerializeObject(CurrentSearchProperties));
            pageState[nameof(CurrentSearchProperties)] = serializedSearchProperties;

            Messenger.Default.Unregister(this);
        }
    }
}
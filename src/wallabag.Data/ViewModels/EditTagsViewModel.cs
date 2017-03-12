using GalaSoft.MvvmLight.Command;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Models;
using wallabag.Data.Services;

namespace wallabag.Data.ViewModels
{
    public class EditTagsViewModel : ViewModelBase
    {
        private readonly IOfflineTaskService _offlineTaskService;
        private readonly ILoggingService _loggingService;
        private readonly SQLite.Net.SQLiteConnection _database;
        private readonly INavigationService _navigationService;

        private IEnumerable<Tag> _previousTags;

        public IList<Item> Items { get; set; } = new List<Item>();
        public ObservableCollection<Tag> Tags { get; set; } = new ObservableCollection<Tag>();
        public ObservableCollection<Tag> Suggestions { get; set; } = new ObservableCollection<Tag>();
        public bool TagsCountIsZero => Tags.Count == 0;

        public ICommand FinishCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public string TagQuery { get; set; }
        public ICommand TagQueryChangedCommand { get; private set; }
        public ICommand TagSubmittedCommand { get; private set; }

        public EditTagsViewModel(IOfflineTaskService offlineTaskService, ILoggingService loggingService, SQLite.Net.SQLiteConnection database, INavigationService navigation)
        {
            _offlineTaskService = offlineTaskService;
            _loggingService = loggingService;
            _database = database;
            _navigationService = navigation;

            _loggingService.WriteLine("Creating new instance of EditTagsViewModel.");

            FinishCommand = new RelayCommand(() => Finish());
            CancelCommand = new RelayCommand(() => Cancel());

            TagQueryChangedCommand = new RelayCommand(() =>
            {
                _loggingService.WriteLine($"Tag query changed: {TagQuery}");

                string suggestionString = TagQuery.ToLower().Split(',').Last();
                _loggingService.WriteLine($"Searching for tags beginning with '{suggestionString}' in the database.");
                Suggestions.Replace(
                    _database.Table<Tag>()
                        .Where(t => t.Label.ToLower().StartsWith(suggestionString))
                        .Except(Tags)
                        .Take(3)
                        .ToList());
            });
            TagSubmittedCommand = new RelayCommand<Tag>(suggestion =>
            {
                _loggingService.WriteLine($"Tag was submitted.");

                if (suggestion != null && !Tags.Contains(suggestion))
                {
                    _loggingService.WriteLine("Tag wasn't in list yet. Added.");
                    Tags.Add(suggestion);
                }
                else if (!string.IsNullOrEmpty(TagQuery))
                {
                    var tags = TagQuery.Split(',').ToList();
                    _loggingService.WriteLine($"Adding {tags.Count} tags to the list.");

                    foreach (string item in tags)
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                        {
                            var newTag = new Tag() { Label = item, Id = Tags.Count + 1 };

                            if (Tags.Contains(newTag) == false)
                                Tags.Add(newTag);
                        }
                    }
                }

                TagQuery = string.Empty;
            });
        }

        public override Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state)
        {
            int itemId = (int)parameter;
            var item = _database.Get<Item>(itemId);

            Items.Add(item);
            _previousTags = item.Tags;
            Tags = new ObservableCollection<Tag>(item.Tags);

            return Task.FromResult(true);
        }

        private void Finish()
        {
            _loggingService.WriteLine($"Editing tags for {Items.Count} items.");

            if (_previousTags == null)
            {
                foreach (var item in Items)
                {
                    _offlineTaskService.Add(item.Id, OfflineTask.OfflineTaskAction.EditTags, Tags.ToList());

                    foreach (var tag in Tags)
                        item.Tags.Add(tag);
                }
            }
            else
            {
                _loggingService.WriteLine($"Number of previous tags: {_previousTags.Count()}");

                var newTags = Tags.Except(_previousTags).ToList();
                var deletedTags = _previousTags.Except(Tags).ToList();

                _loggingService.WriteLine($"Number of new tags: {newTags?.Count}");
                _loggingService.WriteLine($"Number of deleted tags: {deletedTags?.Count}");

                Items.First().Tags.Replace(Tags);

                _offlineTaskService.Add(Items.First().Id, OfflineTask.OfflineTaskAction.EditTags, newTags, deletedTags);
            }

            _navigationService.GoBack();
        }
        private void Cancel()
        {
            _loggingService.WriteLine("Cancelling the editing of tags.");
            _navigationService.GoBack();
        }
    }
}

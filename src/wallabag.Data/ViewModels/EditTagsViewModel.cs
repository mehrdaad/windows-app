using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Models;
using wallabag.Data.Services;
using Windows.UI.Xaml.Controls;

namespace wallabag.Data.ViewModels
{
    public class EditTagsViewModel : ViewModelBase
    {
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

        [PreferredConstructor]
        public EditTagsViewModel()
        {
            _loggingService.WriteLine("Creating new instance of EditTagsViewModel.");

            FinishCommand = new RelayCommand(() => Finish());
            CancelCommand = new RelayCommand(() => Cancel());

            TagQueryChangedCommand = new RelayCommand<AutoSuggestBoxTextChangedEventArgs>(args =>
            {
                _loggingService.WriteLine($"Tag query changed: {TagQuery} (Reason: {args.Reason.ToString()})");
                if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    string suggestionString = TagQuery.ToLower().Split(',').Last();
                    _loggingService.WriteLine($"Searching for tags beginning with '{suggestionString}' in the database.");
                    Suggestions.Replace(
                        _database.Table<Tag>()
                            .Where(t => t.Label.ToLower().StartsWith(suggestionString))
                            .Except(Tags)
                            .Take(3)
                            .ToList());
                }
            });
            TagSubmittedCommand = new RelayCommand<AutoSuggestBoxQuerySubmittedEventArgs>(args =>
            {
                _loggingService.WriteLine($"Tag was submitted. Parameter is {args.ChosenSuggestion?.GetType()?.Name}.");
                var suggestion = args.ChosenSuggestion as Tag;

                if (suggestion != null && !Tags.Contains(suggestion))
                {
                    _loggingService.WriteLine("Tag wasn't in list yet. Added.");
                    Tags.Add(args.ChosenSuggestion as Tag);
                }
                else
                {
                    var tags = args.QueryText.Split(',').ToList();
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
            });
        }
        public EditTagsViewModel(Item item) : this()
        {
            _loggingService.WriteLine($"Creating new instance of EditTagsViewModel for item {item.Id}.");

            Items.Add(item);
            _previousTags = item.Tags;
            Tags = new ObservableCollection<Tag>(item.Tags);
        }

        private void Finish()
        {
            _loggingService.WriteLine($"Editing tags for {Items.Count} items.");
            _loggingService.WriteLineIf(_previousTags != null, $"Number of previous tags: {_previousTags.Count()}");

            if (_previousTags == null)
            {
                foreach (var item in Items)
                {
                    OfflineTaskService.Add(item.Id, OfflineTask.OfflineTaskAction.EditTags, Tags.ToList());

                    foreach (var tag in Tags)
                        item.Tags.Add(tag);
                }
            }
            else
            {
                var newTags = Tags.Except(_previousTags).ToList();
                var deletedTags = _previousTags.Except(Tags).ToList();

                _loggingService.WriteLine($"Number of new tags: {newTags?.Count}");
                _loggingService.WriteLine($"Number of deleted tags: {deletedTags?.Count}");

                Items.First().Tags.Replace(Tags);

                OfflineTaskService.Add(Items.First().Id, OfflineTask.OfflineTaskAction.EditTags, newTags, deletedTags);
            }
        }
        private void Cancel()
        {
            _loggingService.WriteLine("Cancelling the editing of tags.");
        }
    }
}

﻿using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Template10.Mvvm;
using wallabag.Common;
using wallabag.Models;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class EditTagsViewModel : ViewModelBase
    {
        private IEnumerable<Tag> _previousTags;
        public IList<Item> Items { get; set; } = new List<Item>();
        public ObservableCollection<Tag> Tags { get; set; } = new ObservableCollection<Tag>();

        public DelegateCommand FinishCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        public EditTagsViewModel()
        {
            FinishCommand = new DelegateCommand(() => Finish());
            CancelCommand = new DelegateCommand(() => Services.DialogService.HideCurrentDialog());
        }
        public EditTagsViewModel(Item Item)
        {
            FinishCommand = new DelegateCommand(() => Finish());
            CancelCommand = new DelegateCommand(() => Services.DialogService.HideCurrentDialog());

            Items.Add(Item);
            _previousTags = Item.Tags;
            Tags = new ObservableCollection<Tag>(Item.Tags);
        }

        private void Finish()
        {
            if (_previousTags == null)
            {
                foreach (var item in Items)
                {
                    OfflineTask.Add(Items.First().Id, OfflineTask.OfflineTaskAction.EditTags, Tags.ToList());

                    foreach (var tag in Tags)
                        item.Tags.Add(tag);
                }
            }
            else
            {
                var newTags = Tags.Except(_previousTags);
                var deletedTags = _previousTags.Except(Tags);

                Items.First().Tags.Replace(Tags);

                OfflineTask.Add(Items.First().Id, OfflineTask.OfflineTaskAction.EditTags, newTags.ToList(), deletedTags.ToList());
            }
        }
    }
}
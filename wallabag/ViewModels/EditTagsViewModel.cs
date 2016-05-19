using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Api.Models;
using wallabag.Models;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class EditTagsViewModel : ViewModelBase
    {
        private IEnumerable<Tag> _previousTags;
        public IList<Item> Items { get; set; } = new List<Item>();
        public IEnumerable<Tag> Tags { get; set; } = new ObservableCollection<Tag>();

        public DelegateCommand FinishCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        public EditTagsViewModel()
        {
            FinishCommand = new DelegateCommand(async () => await FinishAsync());
            CancelCommand = new DelegateCommand(() => Services.DialogService.HideCurrentDialog());
        }
        public EditTagsViewModel(Item Item)
        {
            FinishCommand = new DelegateCommand(async () => await FinishAsync());
            CancelCommand = new DelegateCommand(() => Services.DialogService.HideCurrentDialog());

            Items.Add(Item);
            _previousTags = Item.Tags;
            Tags = new ObservableCollection<Tag>(Item.Tags);
        }

        private async Task FinishAsync()
        {
            if (_previousTags == null)
            {
                foreach (var item in Items)
                {
                    var results = await App.Client.AddTagsAsync(item.Id, string.Join(",", Tags).Split(","[0]));
                    (item.Tags as List<Tag>).AddRange((IEnumerable<Tag>)results);
                }
            }
            else
            {
                var newTags = Tags.Except(_previousTags);
                var deletedTags = _previousTags.Except(Tags);

                await App.Client.AddTagsAsync(Items.First(), string.Join(",", newTags).Split(","[0]));
                await App.Client.RemoveTagsAsync(Items.First(), (IEnumerable<WallabagTag>)deletedTags);
            }
        }
    }
}

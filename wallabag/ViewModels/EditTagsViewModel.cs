using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Api.Models;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class EditTagsViewModel : ViewModelBase
    {
        private IEnumerable<WallabagTag> _previousTags;
        public IList<WallabagItem> Items { get; set; } = new List<WallabagItem>();
        public IEnumerable<WallabagTag> Tags { get; set; } = new ObservableCollection<WallabagTag>();

        public DelegateCommand FinishCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        public EditTagsViewModel()
        {
            FinishCommand = new DelegateCommand(async () => await FinishAsync());
            CancelCommand = new DelegateCommand(() => Services.DialogService.HideCurrentDialog());
        }
        public EditTagsViewModel(WallabagItem Item)
        {
            FinishCommand = new DelegateCommand(async () => await FinishAsync());
            CancelCommand = new DelegateCommand(() => Services.DialogService.HideCurrentDialog());

            Items.Add(Item);
            _previousTags = Item.Tags;
            Tags = new ObservableCollection<WallabagTag>(Item.Tags);
        }

        private async Task FinishAsync()
        {
            if (_previousTags == null)
            {
                foreach (var item in Items)
                {
                    var results = await App.Client.AddTagsAsync(item.Id, string.Join(",", Tags).Split(","[0]));
                    (item.Tags as List<WallabagTag>).AddRange(results);
                }
            }
            else
            {
                var newTags = Tags.Except(_previousTags);
                var deletedTags = _previousTags.Except(Tags);

                await App.Client.AddTagsAsync(Items.First().Id, string.Join(",", newTags).Split(","[0]));
                await App.Client.RemoveTagsAsync(Items.First().Id, deletedTags.ToArray());
            }
        }
    }
}

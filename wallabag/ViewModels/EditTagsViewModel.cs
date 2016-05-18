using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Api.Models;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class EditTagsViewModel : ViewModelBase
    {
        public IList<WallabagItem> Items { get; set; } = new List<WallabagItem>();
        public IEnumerable<WallabagTag> Tags { get; set; } = new ObservableCollection<WallabagTag>();

        public DelegateCommand FinishCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        public EditTagsViewModel()
        {
            FinishCommand = new DelegateCommand(async () => await FinishAsync());
            CancelCommand = new DelegateCommand(() => Services.DialogService.HideCurrentDialog());
        }

        private async Task FinishAsync()
        {
            foreach (var item in Items)
            {
                var results = await App.Client.AddTagsAsync(item.Id, string.Join(",", Tags).Split(","[0]));
                (item.Tags as List<WallabagTag>).AddRange(results);
            }
        }
    }
}

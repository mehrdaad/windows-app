using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class AddItemViewModel : ViewModelBase
    {
        public string UriString { get; set; } = string.Empty;
        public IEnumerable<string> Tags { get; set; } = new List<string>();
        public string Title { get; set; } = string.Empty;

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        public AddItemViewModel()
        {
            AddCommand = new DelegateCommand(async () => await AddAsync());
            CancelCommand = new DelegateCommand(() => { if (NavigationService.CanGoBack) NavigationService.GoBack(); });
        }

        private async Task<bool> AddAsync()
        {
            var item = await App.Client.AddAsync(new Uri(UriString), Tags.ToArray(), Title);
            return item != null;
        }
    }
}

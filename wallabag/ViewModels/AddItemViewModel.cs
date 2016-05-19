using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Models;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class AddItemViewModel : ViewModelBase
    {
        public string UriString { get; set; } = string.Empty;
        public IEnumerable<Tag> Tags { get; set; } = new ObservableCollection<Tag>();
        public string Title { get; set; } = string.Empty;

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        public AddItemViewModel()
        {
            AddCommand = new DelegateCommand(async () => await AddAsync());
            CancelCommand = new DelegateCommand(() => Services.DialogService.HideCurrentDialog());
        }

        private async Task<bool> AddAsync()
        {
            var item = await App.Client.AddAsync(new Uri(UriString), string.Join(",", Tags).Split(","[0]));
            if (item != null)
            {
                App.Database.Insert(item);
                Messenger.Default.Send(new NotificationMessage("FetchFromDatabase"));
                return true;
            }
            return false;
        }
    }
}

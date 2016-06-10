using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Template10.Mvvm;
using wallabag.Common;
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
            AddCommand = new DelegateCommand(() => Add());
            CancelCommand = new DelegateCommand(() => Services.DialogService.HideCurrentDialog());
        }

        private void Add()
        {
            OfflineTask.Add(UriString, Tags.ToStringArray());

            var uri = new Uri(UriString);
            App.Database.Insert(new Item()
            {
                Id = App.Database.Table<Item>().OrderByDescending(i => i.Id).FirstOrDefault().Id + 1,
                Title = uri.Host,
                Url = UriString,
                Hostname = uri.Host
            });

            Messenger.Default.Send(new NotificationMessage("FetchFromDatabase"));
        }
    }
}

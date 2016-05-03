using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Template10.Mvvm;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        public ICollection<ItemViewModel> Items { get; set; } = new ObservableCollection<ItemViewModel>();

        public DelegateCommand SyncCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }

        public MainViewModel()
        {
            AddCommand = new DelegateCommand(async () => await new Dialogs.AddItemDialog().ShowAsync());
        }
    }
}

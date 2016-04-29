using System.Collections.Generic;
using Template10.Mvvm;

namespace wallabag.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        public ICollection<ItemViewModel> Items { get; set; }
        
        public DelegateCommand SyncCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }

        public MainViewModel()
        {
            
        }
    }
}

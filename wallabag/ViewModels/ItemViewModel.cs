using PropertyChanged;
using Template10.Mvvm;
using wallabag.Api.Models;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class ItemViewModel : ViewModelBase
    {
        public WallabagItem Model { get; private set; }

        public ItemViewModel(WallabagItem Model)
        {
            this.Model = Model;
        }
    }
}
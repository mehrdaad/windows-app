using PropertyChanged;
using System.Collections.Generic;
using System.Threading.Tasks;
using Template10.Mvvm;
using Windows.UI.Xaml.Navigation;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class ItemPageViewModel : ViewModelBase
    {
        public ItemViewModel Item { get; set; }

        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            Item = parameter as ItemViewModel;
            return base.OnNavigatedToAsync(parameter, mode, state);
        }
    }
}

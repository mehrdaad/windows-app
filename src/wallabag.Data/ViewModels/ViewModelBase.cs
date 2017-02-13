using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Data.Common;

namespace wallabag.Data.ViewModels
{
    public class ViewModelBase : GalaSoft.MvvmLight.ViewModelBase, INavigable
    {
        public virtual Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state)
        {
            return Task.FromResult(true);
        }
        public virtual Task OnNavigatedFromAsync(IDictionary<string, object> pageState)
        {
            base.Cleanup();
            return Task.FromResult(true);
        }
    }
}

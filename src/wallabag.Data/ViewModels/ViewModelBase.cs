using GalaSoft.MvvmLight.Ioc;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Data.Common;

namespace wallabag.Data.ViewModels
{
    public class ViewModelBase : GalaSoft.MvvmLight.ViewModelBase, INavigable
    {
        public Dictionary<string, object> SessionState
        {
            get
            {
                if (SimpleIoc.Default.IsRegistered<Dictionary<string, object>>(nameof(SessionState)))
                    return SimpleIoc.Default.GetInstance<Dictionary<string, object>>("SessionState");

                return new Dictionary<string, object>();
            }
        }

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

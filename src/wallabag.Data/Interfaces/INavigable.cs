using System.Collections.Generic;
using System.Threading.Tasks;

namespace wallabag.Data.Common
{
    public interface INavigable
    {
        Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state, NavigationMode navigationMode);
        Task OnNavigatedFromAsync(IDictionary<string, object> pageState);
    }

    public enum NavigationMode { Back, New }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallabag.Data.Services
{
    interface INavigationService : GalaSoft.MvvmLight.Views.INavigationService
    {
        void ClearHistory();
    }
}

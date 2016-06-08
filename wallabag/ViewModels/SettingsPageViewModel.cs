using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]    
    public class SettingsPageViewModel : ViewModelBase
    {
        public bool SyncOnStartup { get; set; }
        public bool AllowTelemetryData { get; set; }
        public bool NavigateBackAfterReadingAnArticle { get; set; }
        public bool SyncReadingProgress { get; set; }
        public DelegateCommand LogoutCommand { get; private set; }
        public DelegateCommand DeleteDatabaseCommand { get; private set; }
    }
}

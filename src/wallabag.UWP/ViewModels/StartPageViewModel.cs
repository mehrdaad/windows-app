using wallabag.Data.ViewModels;

namespace wallabag.ViewModels
{
    public class StartPageViewModel : ViewModelBase
    {
        public bool IsActive { get; set; }
        public string ProgressDescription { get; set; }

        public StartPageViewModel()
        {

        }
    }
}

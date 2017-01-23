namespace wallabag.Data.Services
{
    public interface INavigationService : GalaSoft.MvvmLight.Views.INavigationService
    {
        void ClearHistory();
    }
}

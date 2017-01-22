using System.Threading.Tasks;

namespace wallabag.Data.Services
{
    public interface IDialogService
    {
        Task ShowAsync(string dialogKey, object parameter = null);
        void HideCurrentDialog();
    }
}

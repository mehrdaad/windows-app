using GalaSoft.MvvmLight.Command;
using SQLite.Net;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Api.Models;
using wallabag.Data.Services;

namespace wallabag.Data.ViewModels
{
    public class AnnotationViewModel : ViewModelBase
    {
        private IWallabagClient _client;
        private ILoggingService _logging;
        private SQLiteConnection _database;

        public ItemViewModel Item { get; set; }
        public WallabagAnnotation Annotation { get; set; }
        public string Text { get; set; }

        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand EditCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand DeleteCommand { get; private set; }

        public AnnotationViewModel(
            ItemViewModel item,
            IWallabagClient client,
            ILoggingService logging,
            SQLiteConnection database)
        {
            _client = client;
            _logging = logging;
            _database = database;

            SaveCommand = new RelayCommand(async () => await SaveAsync());
            EditCommand = new RelayCommand(async () => await EditAsync());
            CancelCommand = new RelayCommand(async () => await CancelAsync());
            DeleteCommand = new RelayCommand(async () => await DeleteAsync());
        }
        public void ReplaceAnnotation(int annotationId)
        {
            Annotation = Item.Model.Annotations.FirstOrDefault(x => x.Id == annotationId);
            Text = Annotation.Text;
        }

        public Task SaveAsync() => Task.FromResult(true);
        public Task EditAsync() => Task.FromResult(true);
        public Task CancelAsync() => Task.FromResult(true);
        public Task DeleteAsync() => Task.FromResult(true);
    }
}

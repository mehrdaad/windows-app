using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Template10.Mvvm;
using wallabag.Common;
using wallabag.Models;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml.Navigation;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class AddItemViewModel : ViewModelBase
    {
        private ShareOperation _shareOperation;

        public event EventHandler AddingStarted;

        public string UriString { get; set; } = string.Empty;
        public IEnumerable<Tag> Tags { get; set; } = new ObservableCollection<Tag>();
        public string Title { get; set; } = string.Empty;

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        public AddItemViewModel()
        {
            AddCommand = new DelegateCommand(async () => await Add());
            CancelCommand = new DelegateCommand(() =>
            {
                _shareOperation?.ReportCompleted();
                Services.DialogService.HideCurrentDialog();
            });
        }

        private async Task Add()
        {
            AddingStarted?.Invoke(this, new EventArgs());
            if (_shareOperation != null)
            {
                _shareOperation.ReportStarted();

                var item = await App.Client.AddAsync(new Uri(UriString), Tags.ToStringArray());
                if (item != null)
                {
                    _shareOperation.ReportCompleted();
                    App.Database.Insert((Item)item);
                    return;
                }
            }

            OfflineTask.Add(UriString, Tags.ToStringArray());

            var uri = new Uri(UriString);
            App.Database.Insert(new Item()
            {
                Id = App.Database.Table<Item>().OrderByDescending(i => i.Id).FirstOrDefault().Id + 1,
                Title = uri.Host,
                Url = UriString,
                Hostname = uri.Host
            });

            _shareOperation?.ReportCompleted();
            Messenger.Default.Send(new NotificationMessage("FetchFromDatabase"));
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            var args = SessionState["shareTarget"] as ShareTargetActivatedEventArgs;
            UriString = (await args.ShareOperation.Data.GetWebLinkAsync()).ToString();

            _shareOperation = args.ShareOperation;
        }
    }
}

using GalaSoft.MvvmLight.Command;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Models;
using wallabag.Data.Services;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml.Navigation;

namespace wallabag.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class AddItemViewModel : ViewModelBase
    {
        private ShareOperation _shareOperation;

        public event EventHandler AddingStarted;

        public string UriString { get; set; } = string.Empty;
        public IEnumerable<Tag> Tags { get; set; } = new ObservableCollection<Tag>();
        public string Title { get; set; } = string.Empty;

        public ICommand AddCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public AddItemViewModel()
        {
            AddCommand = new RelayCommand(() => Add());
            CancelCommand = new RelayCommand(() =>
            {
                _shareOperation?.ReportCompleted();
                Services.DialogService.HideCurrentDialog();
            });
        }

        private void Add()
        {
            AddingStarted?.Invoke(this, new EventArgs());

            if (UriString.IsValidUri())
            {
                var uri = new Uri(UriString);
                Database.Insert(new Item()
                {
                    Id = OfflineTaskService.LastItemId + 1,
                    Title = uri.Host,
                    Url = UriString,
                    Hostname = uri.Host
                });

                OfflineTaskService.Add(UriString, Tags.ToStringArray());

                _shareOperation?.ReportCompleted();
            }
        }

        public override Task OnNavigatedToAsync(object parameter, IDictionary<string, object> state)
        {
            return Task.CompletedTask;
            /* TODO: Add implementation for SessionState?
            var args = SessionState["shareTarget"] as ShareTargetActivatedEventArgs;
            UriString = (await args.ShareOperation.Data.GetWebLinkAsync()).ToString();

            _shareOperation = args.ShareOperation;
            */
        }
    }
}

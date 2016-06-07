using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;

namespace wallabag.ViewModels
{
    public class LoginPageViewModel : ViewModelBase
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public DelegateCommand TestConfigurationCommand { get; private set; }
        public DelegateCommand ContinueCommand { get; private set; }

        public LoginPageViewModel()
        {

        }
    }
}

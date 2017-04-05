using wallabag.ViewModels;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Dialogs
{
    [PropertyChanged.ImplementPropertyChanged]
    public sealed partial class LoginDialog : ContentDialog
    {
        private bool _blockClosing = true;
        public LoginDialogViewModel ViewModel { get; private set; }

        public LoginDialog()
        {
            InitializeComponent();
            ViewModel = new LoginDialogViewModel();

            this.Closing += (s, e) => e.Cancel = _blockClosing;

            ViewModel.ReloginCompleted += (s, e) => {
                _blockClosing = false;
                Hide();
            };
        }
    }
}

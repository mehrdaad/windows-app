using wallabag.Data.ViewModels;
using Windows.UI.Xaml.Controls;

namespace wallabag.Dialogs
{
    public sealed partial class AddItemDialog : ContentDialog
    {
        public AddItemViewModel ViewModel => DataContext as AddItemViewModel;

        public AddItemDialog()
        {
            InitializeComponent();
        }
    }
}

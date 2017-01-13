using wallabag.ViewModels;
using Windows.UI.Xaml.Controls;

namespace wallabag.Dialogs
{
    public sealed partial class AddItemDialog : ContentDialog
    {
        public AddItemViewModel ViewModel { get { return DataContext as AddItemViewModel; } }

        public AddItemDialog()
        {
            InitializeComponent();
        }
    }
}

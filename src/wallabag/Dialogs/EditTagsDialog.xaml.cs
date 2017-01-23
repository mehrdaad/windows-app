using wallabag.Data.ViewModels;
using Windows.UI.Xaml.Controls;

namespace wallabag.Dialogs
{
    public sealed partial class EditTagsDialog : ContentDialog
    {
        public EditTagsViewModel ViewModel => DataContext as EditTagsViewModel; 

        public EditTagsDialog()
        {
            InitializeComponent();
        }
    }
}

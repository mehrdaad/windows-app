using wallabag.ViewModels;
using Windows.UI.Xaml.Controls;

namespace wallabag.Dialogs
{
    public sealed partial class EditTagsDialog : ContentDialog
    {
        public EditTagsViewModel ViewModel { get { return this.DataContext as EditTagsViewModel; } }

        public EditTagsDialog()
        {
            this.InitializeComponent();
        }
    }
}

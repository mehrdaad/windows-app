using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace wallabag.Services
{
    public class DialogService
    {
        private static ContentDialog _dialog;

        public static async Task ShowAsync(Dialog dialog, object parameter = null, ElementTheme theme = ElementTheme.Default)
        {
            switch (dialog)
            {
                case Dialog.AddItem:
                    _dialog = new Dialogs.AddItemDialog();
                    break;
                case Dialog.EditTags:
                    _dialog = new Dialogs.EditTagsDialog();
                    break;
            }

            if (parameter != null)
                _dialog.DataContext = parameter;

            _dialog.RequestedTheme = theme;          

            await _dialog?.ShowAsync();
        }
        public static void HideCurrentDialog()
        {
            _dialog?.Hide();
            _dialog = null;
        }

        public enum Dialog
        {
            AddItem,
            EditTags
        }
    }
}

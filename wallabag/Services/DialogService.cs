using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace wallabag.Services
{
    public class DialogService
    {
        private static ContentDialog _dialog;

        public static async Task ShowAsync(Dialog dialog)
        {
            switch (dialog)
            {
                case Dialog.AddItem:
                    _dialog = new Dialogs.AddItemDialog();
                    break;
                case Dialog.EditTags:
                    break;
            }
            await _dialog?.ShowAsync();
        }
        public static void HideCurrentDialog() => _dialog?.Hide();

        public enum Dialog
        {
            AddItem,
            EditTags
        }
    }
}

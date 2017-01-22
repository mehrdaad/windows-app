using System;
using System.Threading.Tasks;
using wallabag.Data.Services;
using Windows.UI.Xaml.Controls;

namespace wallabag.Services
{
    public class DialogService : IDialogService
    {
        private static ContentDialog _dialog;

        public async Task ShowAsync(string dialogKey, object parameter = null)
        {
            switch (dialogKey)
            {
                case Data.Common.Dialogs.AddItemDialog:
                    _dialog = new Dialogs.AddItemDialog();
                    break;
                case Data.Common.Dialogs.EditTagsDialog:
                    _dialog = new Dialogs.EditTagsDialog();
                    break;
            }

            if (parameter != null)
                _dialog.DataContext = parameter;

            // TODO
            //_dialog.RequestedTheme = theme;

            await _dialog?.ShowAsync();
        }
        public void HideCurrentDialog()
        {
            _dialog?.Hide();
            _dialog = null;
        }
    }
}

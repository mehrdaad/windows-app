using GalaSoft.MvvmLight.Messaging;
using SQLite.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using wallabag.Common.Messages;
using wallabag.Models;
using wallabag.Services;

namespace wallabag.Common.Helpers
{
    static class SQLiteConnectionHelper
    {
        private static CancellationTokenSource _cts;

        internal static async Task RunInTransactionWithUndoAsync(
            this SQLiteConnection conn,
            Action a,
            OfflineTask.OfflineTaskAction taskAction,
            int itemCount)
        {
            _cts = new CancellationTokenSource();
            var transactionPoint = conn.SaveTransactionPoint();

            a.Invoke();
            Messenger.Default.Send(new ShowUndoPopupMessage(taskAction, itemCount));

            try { await Task.Delay(SettingsService.Instance.UndoTimeout, _cts.Token); }
            catch (TaskCanceledException) { }

            if (_cts.IsCancellationRequested)
            {
                conn.RollbackTo(transactionPoint);
                // TODO: Inform main view to undo the changes
            }
            else
                conn.Commit();
        }
        internal static void UndoChanges() => _cts.Cancel();
    }
}

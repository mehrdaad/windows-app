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
        private static string _transactionPoint;

        /* Expected behaviour:
         * 1. If there's just one item to change, adapt the change immediately and show a popup
         * 2. If there are multiple items to changes, adapt the changes immediately and show a popup with the correct counter
         * 3. If another change is done while one is still in progress, commit the old one and create a new one.
         * 4. Undoing the changes should be immediately as well.
         * 5. If changes are commited to the database, they should be commited to the server as well.
         */

        internal static async Task RunInTransactionWithUndoAsync(
            this SQLiteConnection conn,
            Action a,
            OfflineTask.OfflineTaskAction taskAction,
            int itemCount = 1)
        {
            bool transactionPointIsBlocked = OfflineTaskService.IsBlocked;
            if (transactionPointIsBlocked == false)
            {
                _cts = new CancellationTokenSource();
                _transactionPoint = conn.SaveTransactionPoint();
                OfflineTaskService.IsBlocked = true;
            }

            a.Invoke();

            if (transactionPointIsBlocked)
                return;

            Messenger.Default.Send(new ShowUndoPopupMessage(taskAction, itemCount));
            Messenger.Default.Send(new ApplyUIUpdatesMessage());

            try { await Task.Delay(SettingsService.Instance.UndoTimeout, _cts.Token); }
            catch (TaskCanceledException) { }

            if (_cts.IsCancellationRequested)
            {
                conn.RollbackTo(_transactionPoint);

                foreach (var item in OfflineTaskService.Queue)
                    item.Invert();

                Messenger.Default.Send(new ApplyUIUpdatesMessage(clearQueue: true));
            }
            else
            {
                conn.Commit();
                OfflineTaskService.Queue.Clear();
            }

            if (transactionPointIsBlocked)
                OfflineTaskService.IsBlocked = false;
        }
        internal static void UndoChanges() => _cts.Cancel();
    }
}

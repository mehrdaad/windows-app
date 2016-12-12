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
            OfflineTaskService.IsBlocked = true;

            _cts = new CancellationTokenSource();
            var transactionPoint = conn.SaveTransactionPoint();

            a.Invoke();
            Messenger.Default.Send(new ShowUndoPopupMessage(taskAction, itemCount));
            Messenger.Default.Send(new ApplyUIUpdatesMessage());

            try { await Task.Delay(SettingsService.Instance.UndoTimeout, _cts.Token); }
            catch (TaskCanceledException) { }

            if (_cts.IsCancellationRequested)
            {
                conn.RollbackTo(transactionPoint);

                foreach (var item in OfflineTaskService.Queue)
                    item.Invert();

                Messenger.Default.Send(new ApplyUIUpdatesMessage(clearQueue: true));
            }
            else
            {
                conn.Commit();
                OfflineTaskService.Queue.Clear();
            }

            OfflineTaskService.IsBlocked = false;
        }
        internal static void UndoChanges() => _cts.Cancel();
    }
}

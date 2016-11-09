using System;
using System.Threading.Tasks;

namespace wallabag.Common.Helpers
{
    static class SQLiteConnectionHelper
    {
        private static bool _resetTransaction = false;
        private static string _transactionPoint = string.Empty;

        internal static void RunInTransactionWithUndo(this SQLite.Net.SQLiteConnection conn, Action action)
        {
            _transactionPoint = conn.SaveTransactionPoint();
            action.Invoke();

            // Wait two seconds for user feedback, the user can reset the changes during this timespan.
            Task.Delay(TimeSpan.FromSeconds(2)).Wait();

            if (_resetTransaction)
                conn.RollbackTo(_transactionPoint);
            else
                conn.Commit();

            _resetTransaction = false;
            _transactionPoint = string.Empty;
        }
        internal static void UndoChanges()
        {
            _resetTransaction = true;
        }
    }
}

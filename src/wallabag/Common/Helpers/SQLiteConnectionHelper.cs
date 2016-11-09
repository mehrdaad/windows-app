using System;
using System.Threading.Tasks;

namespace wallabag.Common.Helpers
{
    static class SQLiteConnectionHelper
    {
        internal static void RunInTransactionWithUndo(this SQLite.Net.SQLiteConnection conn, Action action)
        {
            conn.SaveTransactionPoint();
            action.Invoke();

            // Wait two seconds for user feedback, the user can reset the changes during this timespan.
            Task.Delay(TimeSpan.FromSeconds(2)).Wait();

            conn.Close();
        }
    }
}

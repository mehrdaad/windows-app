using SQLite.Net;
using wallabag.Data.Common;
using wallabag.Data.Models;

namespace wallabag.Tests
{
    class TestsHelper
    {
        public static SQLiteConnection CreateFakeDatabase()
        {
            var db = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), "fakeDatabase.db", serializer: new CustomBlobSerializer());
            db.CreateTable<OfflineTask>();
            db.CreateTable<Tag>();
            db.CreateTable<Item>();
            return db;
        }
    }
}

using SQLite.Net;
using System;
using System.IO;
using wallabag.Data.Common;
using wallabag.Data.Models;

namespace wallabag.Tests
{
    class TestsHelper
    {
        public static SQLiteConnection CreateFakeDatabase()
        {
            Directory.CreateDirectory("fakeDatabases");
            string filename = $"fakeDatabases\\{Guid.NewGuid()}.db";

            var db = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), filename, serializer: new CustomBlobSerializer());
            db.CreateTable<OfflineTask>();
            db.CreateTable<Tag>();
            db.CreateTable<Item>();
            return db;
        }
    }
}

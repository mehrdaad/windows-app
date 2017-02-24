using FakeItEasy;
using GalaSoft.MvvmLight.Ioc;
using SQLite.Net;
using System;
using System.IO;
using wallabag.Data.Common;
using wallabag.Data.Models;
using wallabag.Data.Services;

namespace wallabag.Tests
{
    class TestsHelper
    {
        public static SQLiteConnection CreateFakeDatabase()
        {
            SetupDefaultSettingsService();

            Directory.CreateDirectory("fakeDatabases");
            string filename = $"fakeDatabases\\{Guid.NewGuid()}.db";

            var db = new SQLiteConnection(new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), filename, serializer: new CustomBlobSerializer());
            db.CreateTable<OfflineTask>();
            db.CreateTable<Tag>();
            db.CreateTable<Item>();
            return db;
        }

        private static void SetupDefaultSettingsService()
        {
            try
            {
                bool isRegistered = SimpleIoc.Default.IsRegistered<ISettingsService>();
                if (!isRegistered)
                    SimpleIoc.Default.Register(() => A.Fake<ISettingsService>());
            }
            catch { }
        }
    }
}

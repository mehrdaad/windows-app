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

        internal static void SetupDefaultSettingsService()
        {
            if (SimpleIoc.Default.IsRegistered<ISettingsService>())
                SimpleIoc.Default.Unregister<ISettingsService>();

            SimpleIoc.Default.Register<ISettingsService>(() => A.Fake<ISettingsService>());
        }
    }
}

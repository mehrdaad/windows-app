using System;
using System.Collections.Generic;
using wallabag.Data.Models;

namespace wallabag.Data.Services.MigrationService
{
    public interface IMigrationService
    {
        bool Check(Version oldVersion);
        List<ChangelogEntry> GetChangelog(Version oldVersion);

        void ExecuteAll(Version oldVersion);
        void Add(Migration m);
    }
}

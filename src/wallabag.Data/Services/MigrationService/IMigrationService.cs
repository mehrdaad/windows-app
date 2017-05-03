using System;
using System.Collections.Generic;
using wallabag.Data.Models;

namespace wallabag.Data.Services.MigrationService
{
    public interface IMigrationService
    {
        List<ChangelogEntry> GetChangelog(string previousVersion);

        void ExecuteAll(Version oldVersion);
        void Add(string targetVersion, Action migrationAction, List<ChangelogEntry> changelog);
        void Add(string targetVersion, Action migrationAction, params ChangelogEntry[] changelogEntries);
    }
}

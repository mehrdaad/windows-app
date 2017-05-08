using System;
using System.Collections.Generic;
using wallabag.Data.Models;

namespace wallabag.Data.Services.MigrationService
{
    public class Migration
    {
        public Action Action { get; set; }
        public List<ChangelogEntry> ChangelogEntries { get; set; }
        public Version Version { get; set; }

        public Migration()
        {
            Action = default(Action);
            ChangelogEntries = new List<ChangelogEntry>();
        }

        public static Migration Create(string version) => new Migration() { Version = Version.Parse(version) };
    }

    public static class MigrationExtensions
    {
        public static Migration SetMigrationAction(this Migration m, Action action)
        {
            m.Action = action;
            return m;
        }
        public static void Complete(this Migration m, IMigrationService service) => service?.Add(m);

        public static Migration AddBugfix(this Migration m, string title, string description = "")
        {
            m.ChangelogEntries.Add(new ChangelogEntry(title, description, ChangelogEntry.ChangelogType.Fix));
            return m;
        }
        public static Migration AddFeature(this Migration m, string title, string description = "")
        {
            m.ChangelogEntries.Add(new ChangelogEntry(title, description, ChangelogEntry.ChangelogType.Feature));
            return m;
        }
        public static Migration AddEnhancement(this Migration m, string title, string description = "")
        {
            m.ChangelogEntries.Add(new ChangelogEntry(title, description, ChangelogEntry.ChangelogType.Enhancement));
            return m;
        }
    }
}
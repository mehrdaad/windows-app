using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Data.Models;

namespace wallabag.Data.Services.MigrationService
{
    public class MigrationService : IMigrationService
    {
        private ILoggingService _logging;
        private IPlatformSpecific _device;
        private Dictionary<Version, Action> _migrations;
        private Dictionary<Version, List<ChangelogEntry>> _changelogs;

        public MigrationService(
            ILoggingService loggingService,
            IPlatformSpecific device)
        {
            _logging = loggingService;
            _device = device;
            _migrations = new Dictionary<Version, Action>();
            _changelogs = new Dictionary<Version, List<ChangelogEntry>>();
        }

        public void Add(string version, Action migrationAction, List<ChangelogEntry> changelog)
        {
            _logging.WriteLine($"Add migration for version '{version}'. Number of changelog entries: {changelog?.Count}.");
            if (Version.TryParse(version, out var parsedVersion))
            {
                _migrations.Add(parsedVersion, migrationAction);
                _changelogs.Add(parsedVersion, changelog);
            }
            else
                throw new FormatException($"{nameof(version)} must be a valid version of type Major.Minor.Build.Revision.");
        }
        public void Add(string version, Action migrationAction, params ChangelogEntry[] changelogEntries)
            => Add(version, migrationAction, changelogEntries.ToList());

        public void ExecuteAll(Version oldVersion)
        {
            _logging.WriteLine("Trying to execute all migrations.");

            Version.TryParse(_device.AppVersion, out var newVersion);
            _logging.WriteLine($"Old app version: {oldVersion}");
            _logging.WriteLine($"New app version: {newVersion}");

            if (newVersion == oldVersion)
            {
                _logging.WriteLine("No migrations to execute because the old version and the new one are equal.");
                return;
            }

            var migrations = _migrations
                .Where(v => v.Key > oldVersion)
                .Where(v => v.Key <= newVersion)
                .OrderBy(v => v.Key)
                .ToList();

            _logging.WriteLine($"Number of migrations: {migrations.Count}");

            foreach (var migration in migrations)
            {
                _logging.WriteLine($"Executing migration for version {migration.Key}.");
                migration.Value?.Invoke();
            }
        }
        public List<ChangelogEntry> GetChangelog(Version oldVersion)
        {
            Version.TryParse(_device.AppVersion, out var newVersion);
            _logging.WriteLine($"Returning changelog from {oldVersion} to {newVersion}");

            var changelog = _changelogs
                .Where(v => v.Key > oldVersion)
                .Where(v => v.Key <= newVersion)
                .Select(x => x.Value);

            var result = new List<ChangelogEntry>();

            foreach (var list in changelog)
                result.AddRange(list);

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using wallabag.Data.Interfaces;

namespace wallabag.Data.Services.MigrationService
{
    public class MigrationService : IMigrationService
    {
        private ILoggingService _logging;
        private IPlatformSpecific _device;
        private List<Migration> _migrations;

        private Version _newVersion;

        public MigrationService(
            ILoggingService loggingService,
            IPlatformSpecific device)
        {
            _logging = loggingService;
            _device = device;
            _migrations = new List<Migration>();
        }
        public void Add(Migration m)
        {
            _logging.WriteLine($"Adding migration for version {m.Version}.");
            _migrations.Add(m);
        }

        public bool Check(Version oldVersion)
        {
            if (oldVersion == null)
                return false;

            Version.TryParse(_device.AppVersion, out _newVersion);
            _logging.WriteLine($"Old app version: {oldVersion}");
            _logging.WriteLine($"New app version: {_newVersion}");

            if (_newVersion == oldVersion)
            {
                _logging.WriteLine("No migrations to execute because the old version and the new one are equal.");
                return false;
            }
            return true;
        }

        public void Create(string version, Action action) => Add(new Migration()
        {
            Version = Version.Parse(version),
            Action = action
        });

        public void ExecuteAll(Version oldVersion)
        {
            _logging.WriteLine("Trying to execute all migrations.");

            if (Check(oldVersion))
            {
                var migrations = _migrations
                    .Where(v => v.Version > oldVersion)
                    .Where(v => v.Version <= _newVersion)
                    .OrderBy(v => v.Version)
                    .ToList();

                _logging.WriteLine($"Number of migrations: {migrations.Count}");

                foreach (var migration in migrations)
                {
                    _logging.WriteLine($"Executing migration for version {migration.Version}.");
                    migration.Action?.Invoke();
                }
            }
        }
    }
}

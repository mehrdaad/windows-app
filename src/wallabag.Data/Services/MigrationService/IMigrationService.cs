using System;

namespace wallabag.Data.Services.MigrationService
{
    public interface IMigrationService
    {
        bool Check(Version oldVersion);

        void ExecuteAll(Version oldVersion);
        void Add(Migration m);

        void Create(string version, Action action);
    }
}

using FakeItEasy;
using System;
using wallabag.Data.Interfaces;
using wallabag.Data.Services;
using wallabag.Data.Services.MigrationService;
using Xunit;

namespace wallabag.Tests
{
    public class MigrationServiceTests
    {
        [Fact]
        public void AddingAFeatureThroughTheExtension()
        {
            var testMigration = Migration.Create("1.0.0").AddFeature("test");

            Assert.Equal(1, testMigration.ChangelogEntries.Count);
            Assert.Equal("test", testMigration.ChangelogEntries[0].Title);
            Assert.Equal(testMigration.ChangelogEntries[0].Type, Data.Models.ChangelogEntry.ChangelogType.Feature);

            testMigration.AddFeature("test 2", "my description");

            Assert.Equal(2, testMigration.ChangelogEntries.Count);
            Assert.Equal(testMigration.ChangelogEntries[1].Description, "my description");
            Assert.Equal(testMigration.ChangelogEntries[1].Type, Data.Models.ChangelogEntry.ChangelogType.Feature);
        }

        [Fact]
        public void AddingABugfixThroughTheExtension()
        {
            var testMigration = Migration.Create("1.0.0").AddBugfix("test");

            Assert.Equal(1, testMigration.ChangelogEntries.Count);
            Assert.Equal("test", testMigration.ChangelogEntries[0].Title);
            Assert.Equal(testMigration.ChangelogEntries[0].Type, Data.Models.ChangelogEntry.ChangelogType.Fix);

            testMigration.AddBugfix("test 2", "my description");

            Assert.Equal(2, testMigration.ChangelogEntries.Count);
            Assert.Equal("my description", testMigration.ChangelogEntries[1].Description);
            Assert.Equal(testMigration.ChangelogEntries[1].Type, Data.Models.ChangelogEntry.ChangelogType.Fix);
        }

        [Fact]
        public void AddingAnEnhancementThroughTheExtension()
        {
            var testMigration = Migration.Create("1.0.0").AddEnhancement("test");

            Assert.Equal(1, testMigration.ChangelogEntries.Count);
            Assert.Equal("test", testMigration.ChangelogEntries[0].Title);
            Assert.Equal(testMigration.ChangelogEntries[0].Type, Data.Models.ChangelogEntry.ChangelogType.Enhancement);

            testMigration.AddEnhancement("test 2", "my description");

            Assert.Equal(2, testMigration.ChangelogEntries.Count);
            Assert.Equal(testMigration.ChangelogEntries[1].Description, "my description");
            Assert.Equal(testMigration.ChangelogEntries[1].Type, Data.Models.ChangelogEntry.ChangelogType.Enhancement);
        }

        [Fact]
        public void CompletionThroughExtensionAddsMigrationToService()
        {
            var fakeMigrationService = A.Fake<IMigrationService>();

            Migration.Create("1.0.0").AddEnhancement("test").Complete(fakeMigrationService);

            A.CallTo(() => fakeMigrationService.Add(A<Migration>.Ignored)).MustHaveHappened();
        }

        [Fact]
        public void ChangelogOnlyReturnsEntriesForNewerVersionsThanTheOldOne()
        {
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var migrationService = new MigrationService(logging, device);

            A.CallTo(() => device.AppVersion).Returns("9.0.0");

            for (int i = 1; i <= 10; i++)
                Migration.Create($"{i}.0.0")
                    .AddFeature($"Feature {i}")
                    .Complete(migrationService);

            var changelog = migrationService.GetChangelog(new Version("5.0.0"));

            Assert.Equal(4, changelog.Count);
        }

        [Fact]
        public void ExecutionOnlyExecutesMigrationsNewerThanTheOldVersion()
        {
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var migrationService = new MigrationService(logging, device);

            A.CallTo(() => device.AppVersion).Returns("9.0.0");

            int migrationCounter = 0;
            for (int i = 1; i <= 10; i++)
                Migration.Create($"{i}.0.0")
                    .SetMigrationAction(() => migrationCounter++)
                    .Complete(migrationService);

            migrationService.ExecuteAll(new Version("5.0.0"));
            Assert.Equal(4, migrationCounter);
        }

        [Fact]
        public void MigrationsAreNotExecutedIfVersionsAreEqual()
        {
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var migrationService = new MigrationService(logging, device);

            A.CallTo(() => device.AppVersion).Returns("9.0.0");

            int migrationCounter = 0;
            for (int i = 1; i <= 10; i++)
                Migration.Create($"{i}.0.0")
                    .SetMigrationAction(() => migrationCounter++)
                    .Complete(migrationService);

            migrationService.ExecuteAll(new Version("9.0.0"));
            Assert.Equal(0, migrationCounter);
        }

        [Fact]
        public void MigrationsAreExecutedFromOldestToNewest()
        {
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var migrationService = new MigrationService(logging, device);

            A.CallTo(() => logging.WriteLine(
                A<string>.Ignored,
                A<LoggingCategory>.Ignored,
                A<string>.Ignored,
                A<int>.Ignored))
                .Invokes(x => System.Diagnostics.Debug.WriteLine(x.Arguments.Get<string>(0)));

            A.CallTo(() => device.AppVersion).Returns("9.0.0");

            int currentMajorVersion = 0;
            Migration.Create($"4.0.0")
                .SetMigrationAction(() => currentMajorVersion = 4)
                .Complete(migrationService);
            Migration.Create($"2.0.0")
                .SetMigrationAction(() => currentMajorVersion = 2)
                .Complete(migrationService);
            Migration.Create($"3.0.0")
                .SetMigrationAction(() => currentMajorVersion = 3)
                .Complete(migrationService);

            migrationService.ExecuteAll(new Version("1.0.0"));
            Assert.Equal(4, currentMajorVersion);
        }

        [Fact]
        public void NotExistingChangelogForSpecificVersionReturnsEmptyList()
        {
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var migrationService = new MigrationService(logging, device);

            A.CallTo(() => device.AppVersion).Returns("9.0.0");

            for (int i = 1; i <= 6; i++)
                Migration.Create($"{6 - i}.0.0")
                    .AddBugfix("My bugfix")
                    .Complete(migrationService);

            var changelog = migrationService.GetChangelog(new Version("8.0.0"));

            Assert.NotNull(changelog);
            Assert.Equal(0, changelog.Count);
        }
    }
}

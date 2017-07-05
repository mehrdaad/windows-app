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
        public void ExecutionOnlyExecutesMigrationsNewerThanTheOldVersion()
        {
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var migrationService = new MigrationService(logging, device);

            A.CallTo(() => device.AppVersion).Returns("9.0.0");

            int migrationCounter = 0;
            for (int i = 1; i <= 10; i++)
                migrationService.Create($"{i}.0.0", () => migrationCounter++);

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
                migrationService.Create($"{i}.0.0", () => migrationCounter++);

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
            migrationService.Create($"4.0.0", () => currentMajorVersion = 4);
            migrationService.Create($"2.0.0", () => currentMajorVersion = 2);
            migrationService.Create($"3.0.0", () => currentMajorVersion = 3);

            migrationService.ExecuteAll(new Version("1.0.0"));
            Assert.Equal(4, currentMajorVersion);
        }
    }
}

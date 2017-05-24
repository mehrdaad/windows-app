using FakeItEasy;
using System.Collections.Generic;
using wallabag.Data.Services;
using wallabag.Data.Services.OfflineTaskService;
using wallabag.Data.ViewModels;
using Xunit;

namespace wallabag.Tests
{
    public class AddItemViewModelTests
    {
        [Fact]
        public void CancellingWillNavigateBack()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new AddItemViewModel(offlineTaskService, loggingService, database, navigationService);

            viewModel.CancelCommand.Execute(null);

            A.CallTo(() => navigationService.GoBack()).MustHaveHappened();
        }

        [Fact]
        public void AddingAnUriDoesExecuteTheOfflineTaskServiceAndNavigatesBack()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new AddItemViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                UriString = "http://wallabag.org"
            };
            viewModel.AddCommand.Execute(null);

            A.CallTo(() => navigationService.GoBack()).MustHaveHappened();
            A.CallTo(() => offlineTaskService.AddAsync(A<string>.Ignored, A<IEnumerable<string>>.Ignored)).MustHaveHappened();
            // TODO: Add database check
        }

        [Fact]
        public void AddingAnMicrosoftEdgeUriAddsTheCorrectOne()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new AddItemViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                UriString = "read:http://wallabag.org"
            };
            viewModel.AddCommand.Execute(null);

            A.CallTo(() => navigationService.GoBack()).MustHaveHappened();
            A.CallTo(() => offlineTaskService.AddAsync(A<string>.That.IsEqualTo("http://wallabag.org"), A<IEnumerable<string>>.Ignored)).MustHaveHappened();
            // TODO: Add database check
        }

        [Fact]
        public void AddingAnInvalidUriDoesNotExecuteTheOfflineTaskService()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new AddItemViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                UriString = "fake"
            };
            viewModel.AddCommand.Execute(null);

            A.CallTo(() => navigationService.GoBack()).MustNotHaveHappened();
            A.CallTo(() => offlineTaskService.AddAsync(A<string>.Ignored, A<IEnumerable<string>>.Ignored)).MustNotHaveHappened();
            // TODO: Add database check
        }
    }
}

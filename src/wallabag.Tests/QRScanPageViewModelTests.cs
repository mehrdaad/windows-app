using FakeItEasy;
using System;
using wallabag.Data.Interfaces;
using wallabag.Data.Services;
using wallabag.Data.ViewModels;
using Xunit;
using ZXing;

namespace wallabag.Tests
{
    public class QRScanPageViewModelTests
    {
        [Fact]
        public void SettingAValidScanResultNavigatesBack()
        {
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var logging = A.Fake<ILoggingService>();

            var viewModel = new QRScanPageViewModel(device, navigation, logging);
            viewModel.LastScanResult = new Result(
                "wallabag://test@test.de",
                Array.Empty<byte>(),
                Array.Empty<ResultPoint>(),
                BarcodeFormat.QR_CODE);

            A.CallTo(() => navigation.GoBack()).MustHaveHappened();
        }

        [Theory]
        [InlineData("wallllllllllabag://test@test.de")]
        [InlineData("fake://test@test.de")]
        [InlineData("wallabag//test@test.de")]
        [InlineData("wallabag:/test@test.de")]
        [InlineData("wallabag://testtest.de")]
        [InlineData("wallabagtest@test.de")]
        public void SettingAnInvalidScanResultDoesNotNavigateBack(string schema)
        {
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var logging = A.Fake<ILoggingService>();

            var viewModel = new QRScanPageViewModel(device, navigation, logging);
            viewModel.LastScanResult = new Result(
                schema,
                Array.Empty<byte>(),
                Array.Empty<ResultPoint>(),
                BarcodeFormat.QR_CODE);

            A.CallTo(() => navigation.GoBack()).MustNotHaveHappened();
        }
    }
}

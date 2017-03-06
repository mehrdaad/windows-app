using System;
using System.Text.RegularExpressions;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Interfaces;
using wallabag.Data.Services;
using ZXing;
using ZXing.Mobile;

namespace wallabag.Data.ViewModels
{
    public class QRScanPageViewModel : ViewModelBase
    {
        private readonly IPlatformSpecific _device;
        private readonly INavigationService _navigation;
        private readonly ILoggingService _logging;

        public MobileBarcodeScanner Scanner { get; private set; }
        public MobileBarcodeScanningOptions ScanOptions => new ZXing.Mobile.MobileBarcodeScanningOptions()
        {
            TryHarder = false,
            PossibleFormats = new System.Collections.Generic.List<ZXing.BarcodeFormat>() { ZXing.BarcodeFormat.QR_CODE }
        };
        public string Description { get; private set; }

        public const string m_QRRESULTKEY = "QRResult";

        private Result _lastScanResult;
        public Result LastScanResult
        {
            get => _lastScanResult;
            set
            {
                _lastScanResult = value;
                ScanResultChanged();
            }
        }

        public QRScanPageViewModel(
            IPlatformSpecific device,
            INavigationService navigation,
            ILoggingService logging)
        {
            _device = device;
            _navigation = navigation;
            _logging = logging;

            Description = _device.GetLocalizedResource("HoldCameraOntoQRCodeMessage");
        }

        private void ScanResultChanged()
        {
            _logging.WriteLine("Scan result changed.");
            if (ProtocolHelper.Validate(_lastScanResult?.Text))
            {
                _logging.WriteLine($"Scan result ('{_lastScanResult?.Text}') is valid.");
                var result = ProtocolHelper.Parse(_lastScanResult?.Text);

                _logging.WriteLine($"Resulted server: {result.Server}");
                _logging.WriteLine($"Resulted username: {result.Username}");

                SessionState.Add(m_QRRESULTKEY, result);
                _navigation.GoBack();
            }
        }
    }
}

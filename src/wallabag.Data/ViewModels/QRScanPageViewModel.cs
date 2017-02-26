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

        public MobileBarcodeScanner Scanner { get; private set; }
        public MobileBarcodeScanningOptions ScanOptions => new ZXing.Mobile.MobileBarcodeScanningOptions()
        {
            TryHarder = false,
            PossibleFormats = new System.Collections.Generic.List<ZXing.BarcodeFormat>() { ZXing.BarcodeFormat.QR_CODE }
        };
        public string Description { get; private set; }

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

        public QRScanPageViewModel(IPlatformSpecific device, INavigationService navigation)
        {
            _device = device;
            _navigation = navigation;

            Description = _device.GetLocalizedResource("HoldCameraOntoQRCodeMessage");
        }

        private void ScanResultChanged()
        {
            if (ProtocolHelper.Validate(_lastScanResult?.Text))
            {
                _navigation.GoBack();

                // TODO
                //SessionState.Add(QRResultKey, ProtocolHelper.Parse(result?.Text));
                //Dispatcher.Dispatch(() => NavigationService.GoBack());
            }
        }
    }
}

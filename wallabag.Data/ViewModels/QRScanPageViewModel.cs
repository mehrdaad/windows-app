using System.Collections.Generic;
using Template10.Mvvm;
using wallabag.Data.Common.Helpers;
using ZXing.Mobile;

namespace wallabag.Data.ViewModels
{
    public class QRScanPageViewModel : ViewModelBase
    {
        private ZXingScannerControl scannerControl;

        public DelegateCommand ScanCommand { get; private set; }
        public string ScanResult { get; set; }

        public const string QRResultKey = "QRScanResult";

        public QRScanPageViewModel()
        {
            ScanCommand = new DelegateCommand(async () => await scannerControl.StartScanningAsync(result =>
            {
                bool success = string.IsNullOrEmpty(result?.Text) == false && result.Text.StartsWith("wallabag://");

                if (success)
                {
                    SessionState.Add(QRResultKey, ProtocolHelper.Parse(result?.Text));
                    Dispatcher.Dispatch(() => NavigationService.GoBack());
                }
            },
            new MobileBarcodeScanningOptions()
            {
                TryHarder = false,
                PossibleFormats = new List<ZXing.BarcodeFormat>() { ZXing.BarcodeFormat.QR_CODE }
            }));
        }
        public QRScanPageViewModel(ZXingScannerControl scannerControl) : this()
        {
            this.scannerControl = scannerControl;
            ScanCommand.Execute();
        }
    }
}

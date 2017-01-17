using System.Collections.Generic;
using Template10.Mvvm;
using ZXing.Mobile;

namespace wallabag.ViewModels
{
    public class QRScanPageViewModel : ViewModelBase
    {
        private ZXingScannerControl scannerControl;

        public DelegateCommand ScanCommand { get; private set; }
        public string ScanResult { get; set; }

        public const string QRSuccessKey = "QRScanWasSuccessful";
        public const string QRResultKey = "QRScanWasSuccessful";

        public QRScanPageViewModel()
        {
            ScanCommand = new DelegateCommand(async () => await scannerControl.StartScanningAsync(result =>
            {
                bool success = string.IsNullOrEmpty(result?.Text) == false && result.Text.StartsWith("wallabag://");

                SessionState.Add(QRSuccessKey, success);
                if (success)
                    SessionState.Add(QRResultKey, result?.Text);
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
        }
    }
}

using GalaSoft.MvvmLight.Command;
using System.Collections.Generic;
using System.Windows.Input;
using wallabag.Data.Common.Helpers;
using ZXing.Mobile;

namespace wallabag.Data.ViewModels
{
    public class QRScanPageViewModel : ViewModelBase
    {
        private ZXingScannerControl scannerControl;

        public ICommand ScanCommand { get; private set; }
        public string ScanResult { get; set; }

        public const string QRResultKey = "QRScanResult";

        public QRScanPageViewModel()
        {
            ScanCommand = new RelayCommand(async () => await scannerControl.StartScanningAsync(result =>
            {
                bool success = string.IsNullOrEmpty(result?.Text) == false && result.Text.StartsWith("wallabag://");

                if (success)
                {
                    // TODO
                    //SessionState.Add(QRResultKey, ProtocolHelper.Parse(result?.Text));
                    //Dispatcher.Dispatch(() => NavigationService.GoBack());
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

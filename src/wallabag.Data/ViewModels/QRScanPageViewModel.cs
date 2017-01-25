using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        [PreferredConstructor]
        public QRScanPageViewModel()
        {
            LoggingService.WriteLine($"Creating new instance of {nameof(QRScanPageViewModel)}.");
            ScanCommand = new RelayCommand(async () => await ScanAsync());
        }
        public QRScanPageViewModel(ZXingScannerControl scannerControl) : this()
        {
            LoggingService.WriteLine($"Creating new instance of {nameof(QRScanPageViewModel)} with given scanner control.");

            this.scannerControl = scannerControl;
            ScanCommand.Execute();
        }

        private async Task ScanAsync()
        {
            LoggingService.WriteLine("Scanning for wallabag QR code...");
            await scannerControl.StartScanningAsync(result =>
            {
                LoggingService.WriteLine($"QR code found. Text: {result?.Text}");
                bool success = string.IsNullOrEmpty(result?.Text) == false && result.Text.StartsWith("wallabag://");

                if (success)
                {
                    LoggingService.WriteLine("QR code matches the protocol.");
                    // TODO
                    //SessionState.Add(QRResultKey, ProtocolHelper.Parse(result?.Text));
                    //Dispatcher.Dispatch(() => NavigationService.GoBack());
                }
            },
            new MobileBarcodeScanningOptions()
            {
                TryHarder = false,
                PossibleFormats = new List<ZXing.BarcodeFormat>() { ZXing.BarcodeFormat.QR_CODE }
            });
        }
    }
}

using wallabag.Data.Common.Helpers;
using wallabag.Data.ViewModels;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class QRScanPage : Page
    {
        public QRScanPage()
        {
            InitializeComponent();

            // TODO: Create a ViewModel for the QRScanPage       
            //DataContext = new QRScanPageViewModel(scannerControl);
            //scannerControl.TopText = GeneralHelper.LocalizedResource("HoldCameraOntoQRCodeMessage");
        }
    }
}

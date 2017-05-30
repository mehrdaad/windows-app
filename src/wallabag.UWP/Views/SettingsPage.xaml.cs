using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Data.ViewModels;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPageViewModel ViewModel => DataContext as SettingsPageViewModel;

        public SettingsPage() => InitializeComponent();

        private void VideoOpenModeRadioButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (sender == BrowserVideoOpenModeRadioButton)
                ViewModel.SetVideoOpenMode(Data.Common.Settings.Reading.WallabagVideoOpenMode.Browser);
            else if (sender == AppVideoOpenModeRadioButton)
                ViewModel.SetVideoOpenMode(Data.Common.Settings.Reading.WallabagVideoOpenMode.App);
            else if (sender == InlineVideoOpenModeRadioButton)
                ViewModel.SetVideoOpenMode(Data.Common.Settings.Reading.WallabagVideoOpenMode.Inline);
        }

        private async Task ComposeEmailAsync()
        {
            /*
             * Taken from Martin Suchan'z blog: https://www.suchan.cz/2015/08/uwp-quick-tip-getting-device-os-and-app-info/
             */
            string deviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;

            // get the system version number, e.g. 
            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = (v & 0x000000000000FFFFL);
            string systemVersion = $"{v1}.{v2}.{v3}.{v4}";

            // get the package architecure
            string architecture = Package.Current.Id.Architecture.ToString().ToLower();

            var bodyLines = new List<string>
            {
                "Explain your issue below in English or German:\r\n\r\n\r\n\r\n",
                $"App version: {ViewModel.VersionNumber}",
                $"Device family: {deviceFamily}",
                $"OS version: {systemVersion}",
                $"Architecture: {architecture}",
            };
            var emailMessage = new Windows.ApplicationModel.Email.EmailMessage()
            {
                Body = string.Join("\r\n", bodyLines),
                Subject = $"Issue with wallabag {ViewModel.VersionNumber}"
            };

            // Limit the size of the log to 1000 entries
            var originLogFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("log.txt", CreationCollisionOption.OpenIfExists);
            var log = await FileIO.ReadLinesAsync(originLogFile);
            var cuttedLog = log.Skip(Math.Max(0, log.Count - 1000));

            var cuttedLogFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("cutted-log.txt", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteLinesAsync(cuttedLogFile, cuttedLog);

            var stream = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(originLogFile);
            var attachment = new Windows.ApplicationModel.Email.EmailAttachment(cuttedLogFile.Name, stream);
            emailMessage.Attachments.Add(attachment);

            var email = new Windows.ApplicationModel.Contacts.ContactEmail()
            {
                Address = "jlnostr+wallabag@outlook.de",
                Kind = Windows.ApplicationModel.Contacts.ContactEmailKind.Work
            };
            if (email != null)
            {
                var emailRecipient = new Windows.ApplicationModel.Email.EmailRecipient(email.Address, "App support (Julian)");
                emailMessage.To.Add(emailRecipient);
            }

            await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }

        private async void ContactDeveloperButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) => await ComposeEmailAsync();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var client = new Api.WallabagClient(new Uri("https://wallabag.jlnostr.de"), "1_4xy4khl22uck0o8gcs4kw4cwg80s88os0kw0k4so4ssg804ogk", "5wakw2t7uxkwsww480swooow8gsgko8w40wso0w8c4gocsc4ws");
            await client.RequestTokenAsync("jlnostr", "w8DDidGsT.");
            var version = await client.GetVersionNumberAsync();
            System.Diagnostics.Debug.WriteLine(version);
        }
    }
}

using System;
using System.Threading.Tasks;
using Template10.Common;
using Windows.ApplicationModel.Activation;

namespace wallabag
{
    public sealed partial class App : BootStrapper
    {
        public static Api.WallabagClient Client { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }
        
        public override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            if (startKind == StartKind.Launch)
            {
                NavigationService.Navigate(typeof(Views.MainPage));
            }
            return Task.CompletedTask;
        }
    }
}

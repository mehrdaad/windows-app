using GalaSoft.MvvmLight.Ioc;
using PropertyChanged;
using System;
using wallabag.Data.Interfaces;

namespace wallabag.Data.Models
{
    [ImplementPropertyChanged]
    public class WallabagProvider
    {
        private static IPlatformSpecific _device => SimpleIoc.Default.GetInstance<IPlatformSpecific>();

        public string Name { get; set; }
        public Uri Url { get; set; }
        public string ShortDescription { get; set; }
        public static WallabagProvider Other { get; } = new WallabagProvider(default(Uri), _device.GetLocalizedResource("OtherProviderName"), _device.GetLocalizedResource("OtherProviderDescription"));

        public WallabagProvider(Uri url, string name, string shortDescription = "")
        {
            Url = url;
            Name = name;
            ShortDescription = shortDescription;
        }
    }
}

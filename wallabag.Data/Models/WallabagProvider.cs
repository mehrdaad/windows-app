using PropertyChanged;
using System;
using wallabag.Data.Common.Helpers;

namespace wallabag.Data.Models
{
    [ImplementPropertyChanged]
    public class WallabagProvider
    {
        public string Name { get; set; }
        public Uri Url { get; set; }
        public string ShortDescription { get; set; }
        public static WallabagProvider Other { get; } = new WallabagProvider(default(Uri), GeneralHelper.LocalizedResource("OtherProviderName"), GeneralHelper.LocalizedResource("OtherProviderDescription"));

        public WallabagProvider(Uri url, string name, string shortDescription = "")
        {
            Url = url;
            Name = name;
            ShortDescription = shortDescription;
        }
    }
}

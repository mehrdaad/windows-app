using PropertyChanged;
using System;

namespace wallabag.Data.Models
{
    [ImplementPropertyChanged]
    public class WallabagProvider
    {
        public string Name { get; set; }
        public Uri Url { get; set; }
        public string ShortDescription { get; set; }
        public static WallabagProvider Other { get; } = new WallabagProvider(default(Uri), string.Empty);

        public WallabagProvider(Uri url, string name, string shortDescription = "")
        {
            Url = url;
            Name = name;
            ShortDescription = shortDescription;
        }
    }
}

using PropertyChanged;
using System;

namespace wallabag.Models
{
    [ImplementPropertyChanged]
    public class WallabagProvider
    {
        public string Name { get; set; }
        public Uri Url { get; set; }
        public string ShortDescription { get; set; }

        public WallabagProvider(Uri url, string name, string shortDescription = "")
        {
            Url = url;
            Name = name;
            ShortDescription = shortDescription;
        }
    }
}

using PropertyChanged;
using System;
using wallabag.Data.Interfaces;

namespace wallabag.Data.Models
{
    [ImplementPropertyChanged]
    public class WallabagProvider
    {
        public string Name { get; set; }
        public Uri Url { get; set; }
        public string ShortDescription { get; set; }

        public static WallabagProvider GetOther(IPlatformSpecific device)
            => new WallabagProvider(
                default(Uri),
                device.GetLocalizedResource("OtherProviderName"),
                device.GetLocalizedResource("OtherProviderDescription"));

        public WallabagProvider(Uri url, string name, string shortDescription = "")
        {
            Url = url;
            Name = name;
            ShortDescription = shortDescription;
        }

        public override bool Equals(object obj)
        {
            var p = obj as WallabagProvider;

            if (p != null)
                return Url == p.Url && Name.Equals(p.Name);

            return false;
        }

        public override int GetHashCode() => Url.GetHashCode();
    }
}

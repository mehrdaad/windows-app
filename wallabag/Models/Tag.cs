using PropertyChanged;
using SQLite.Net.Attributes;
using wallabag.Api.Models;

namespace wallabag.Models
{
    [ImplementPropertyChanged]
    public class Tag
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Label { get; set; }
        public string Slug { get; set; }

        public static implicit operator WallabagTag(Tag t)
        {
            return new WallabagTag()
            {
                Id = t.Id,
                Label = t.Label,
                Slug = t.Slug
            };
        }
        public static implicit operator Tag(WallabagTag t)
        {
            return new Tag()
            {
                Id = t.Id,
                Label = t.Label,
                Slug = t.Slug
            };
        }
    }
}

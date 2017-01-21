using PropertyChanged;
using SQLite.Net.Attributes;
using System;
using wallabag.Api.Models;

namespace wallabag.Data.Models
{
    [ImplementPropertyChanged]
    public class Tag : IComparable
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Label { get; set; }
        public string Slug { get; set; }

        public override string ToString() => Label;
        public override int GetHashCode() => Id;
        public override bool Equals(object obj) => Label == (obj as Tag).Label;

        public int CompareTo(object obj)
        {
            if (obj is Tag)
                return ((IComparable)Label).CompareTo((obj as Tag).Label);
            else
                return 0;
        }

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

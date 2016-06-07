using PropertyChanged;
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using wallabag.Api.Models;

namespace wallabag.Models
{
    [ImplementPropertyChanged]
    public class Item : IComparable
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Title { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public IEnumerable<Tag> Tags { get; set; }

        [Indexed]
        public bool IsRead { get; set; }
        [Indexed]
        public bool IsStarred { get; set; }
        [Indexed]
        public DateTime CreationDate { get; set; }
        [Indexed]
        public DateTime LastModificationDate { get; set; }
        [Indexed]
        public int EstimatedReadingTime { get; set; }
        public string Hostname { get; set; }
        [Indexed]
        public string Language { get; set; }
        [Indexed]
        public string Mimetype { get; set; }
        public Uri PreviewImageUri { get; set; }
        public double ReadingProgress { get; set; }

        public static implicit operator WallabagItem(Item i)
        {
            List<WallabagTag> convertedTags = new List<WallabagTag>();
            foreach (var item in i.Tags)
                convertedTags.Add(item);

            return new WallabagItem()
            {
                Id = i.Id,

                Title = i.Title,
                Content = i.Content,
                Url = i.Url,

                IsRead = i.IsRead,
                IsStarred = i.IsStarred,
                CreationDate = i.CreationDate.ToUniversalTime(),
                LastUpdated = i.LastModificationDate.ToUniversalTime(),
                EstimatedReadingTime = i.EstimatedReadingTime,
                DomainName = i.Hostname,
                Language = i.Language,
                Mimetype = i.Mimetype,
                PreviewImageUri = i.PreviewImageUri,
                Tags = convertedTags
            };
        }
        public static implicit operator Item(WallabagItem i)
        {
            List<Tag> convertedTags = new List<Tag>();
            foreach (var item in i.Tags)
                convertedTags.Add(item);

            return new Item()
            {
                Id = i.Id,

                Title = i.Title,
                Content = i.Content,
                Url = i.Url,

                IsRead = i.IsRead,
                IsStarred = i.IsStarred,
                CreationDate = i.CreationDate.ToUniversalTime(),
                LastModificationDate = i.LastUpdated.ToUniversalTime(),
                EstimatedReadingTime = i.EstimatedReadingTime,
                Hostname = i.DomainName,
                Language = i.Language,
                Mimetype = i.Mimetype,
                PreviewImageUri = i.PreviewImageUri,
                Tags = convertedTags
            };
        }
        public int CompareTo(object obj) => CreationDate.CompareTo((obj as Item).CreationDate);
        public override string ToString() => Title ?? string.Empty;
        public override bool Equals(object obj)
        {
            if (obj != null && obj.GetType().Equals(typeof(Item)))
            {
                var comparedItem = obj as Item;
                return Id.Equals(comparedItem.Id) && LastModificationDate.Equals(comparedItem.LastModificationDate);
            }
            return false;
        }
        public override int GetHashCode() => Id;
    }
}

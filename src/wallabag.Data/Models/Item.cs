using PropertyChanged;
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using wallabag.Api.Models;

namespace wallabag.Data.Models
{
    [ImplementPropertyChanged]
    public class Item : IComparable
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; }
        public ObservableCollection<Tag> Tags { get; set; } = new ObservableCollection<Tag>();

        [Indexed]
        public bool IsRead { get; set; } = false;
        [Indexed]
        public bool IsStarred { get; set; } = false;
        [Indexed]
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        [Indexed]
        public DateTime LastModificationDate { get; set; } = DateTime.UtcNow;
        [Indexed]
        public int EstimatedReadingTime { get; set; }
        public string Hostname { get; set; }
        [Indexed]
        public string Language { get; set; }
        public string Mimetype { get; set; }
        public Uri PreviewImageUri { get; set; }
        [Indexed]
        public double ReadingProgress { get; set; }

        public static implicit operator WallabagItem(Item i)
        {
            var convertedTags = new List<WallabagTag>();
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
            var convertedTags = new ObservableCollection<Tag>();

            if (i?.Tags != null)
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
        public int CompareTo(object obj)
        {
            if (obj is Item objectToCompare)
            {
                int creationDateComparison = CreationDate.CompareTo(objectToCompare.CreationDate);
                if (creationDateComparison == 0)
                    return Id.CompareTo(objectToCompare.Id);
                else
                    return creationDateComparison;
            }
            else
                throw new ArgumentException($"An {nameof(Item)} cannot be compared with a {obj?.GetType().FullName ?? null}.");
        }
        public override string ToString() => Title ?? string.Empty;
        public override bool Equals(object obj)
        {
            if (obj is Item comparedItem)
            {
                bool idEquals = Id.Equals(comparedItem.Id);
                bool modificationDateEquals = LastModificationDate.Equals(comparedItem.LastModificationDate);

                return idEquals && modificationDateEquals;
            }
            return false;
        }
        public override int GetHashCode() => Id;
    }
}

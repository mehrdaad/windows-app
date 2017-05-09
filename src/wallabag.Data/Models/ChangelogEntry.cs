namespace wallabag.Data.Models
{
    public class ChangelogEntry
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public ChangelogType Type { get; set; }

        public ChangelogEntry()
        {
            Title = string.Empty;
            Description = string.Empty;
            Type = ChangelogType.Fix;
        }
        public ChangelogEntry(string title, string description, ChangelogType type)
        {
            Title = title;
            Description = description;
            Type = type;
        }

        public enum ChangelogType
        {
            Fix,
            Feature,
            Enhancement
        }
    }
}

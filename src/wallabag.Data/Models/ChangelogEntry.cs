namespace wallabag.Data.Models
{
    public class ChangelogEntry
    {
        public string Label { get; set; }
        public ChangelogType Type { get; set; }

        public ChangelogEntry()
        {
            Label = string.Empty;
            Type = ChangelogType.Fix;
        }
        public ChangelogEntry(ChangelogType type, string label)
        {
            Label = label;
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

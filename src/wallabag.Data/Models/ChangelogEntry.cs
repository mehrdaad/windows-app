namespace wallabag.Data.Models
{
    public class ChangelogEntry
    {
        public string Label { get; set; }
        public ChangelogType Type { get; set; }

        public enum ChangelogType
        {
            Fix,
            Feature,
            Enhancement
        }
    }
}

namespace wallabag.Data.Models
{
    public class ClientCreationData
    {
        public bool Success => !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Secret);

        public string Id { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}

using Newtonsoft.Json;

namespace wallabag.Api.Models
{
    class WallabagTag
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        public override string ToString() => this.Label;
    }
}

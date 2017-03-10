using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using wallabag.Data.Models;

namespace wallabag.Data.Common
{
    public class JsonTagConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(ObservableCollection<Tag>);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new List<Tag>();

            var tagArray = JArray.Load(reader);
            foreach (var item in tagArray)
            {
                int id = (int)item["Id"];
                string label = (string)item["Label"];
                string slug = (string)item["Slug"];

                result.Add(new Tag()
                {
                    Id = id,
                    Label = label,
                    Slug = slug
                });
            }

            return new ObservableCollection<Tag>(result);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var collection = value as ObservableCollection<Tag>;

            var result = new JArray();
            foreach (var item in collection)
            {
                var jsonTag = new JObject
                {
                    new JProperty("Id", item.Id),
                    new JProperty("Label", item.Label),
                    new JProperty("Slug", item.Slug)
                };
                result.Add(jsonTag);
            }

            result.WriteTo(writer);
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using wallabag.Api.Models;

namespace wallabag.Data.Common.JsonConverters
{
    public class JsonAnnotationConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(List<WallabagAnnotation>);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new List<WallabagAnnotation>();

            var tagArray = JArray.Load(reader);
            foreach (var item in tagArray)
            {
                int id = (int)item["Id"];
                string label = (string)item["Text"];
                var arr = (JArray)item["Range"];

                var range = new WallabagAnnotationRange()
                {
                    Start = (string)arr[0],
                    StartOffset = (int)arr[1],
                    End = (string)arr[2],
                    EndOffset = (int)arr[3]
                };

                result.Add(new WallabagAnnotation(range, label) { Id = id });
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var collection = value as List<WallabagAnnotation>;

            var result = new JArray();
            foreach (var item in collection)
            {
                var json = new JObject
                {
                    new JProperty("Id", item.Id),
                    new JProperty("Text", item.Text),
                    new JProperty("Range",
                        item.Range.Start,
                        item.Range.StartOffset,
                        item.Range.End,
                        item.Range.EndOffset
                    )
                };
                result.Add(json);
            }

            result.WriteTo(writer);
        }
    }
}

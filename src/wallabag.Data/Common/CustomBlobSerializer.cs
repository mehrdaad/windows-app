using Newtonsoft.Json;
using SQLite.Net;
using System;

namespace wallabag.Data.Common
{
    public class CustomBlobSerializer : IBlobSerializer
    {
        private JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public bool CanDeserialize(Type type) => true;

        public object Deserialize(byte[] data, Type type)
        {
            string str = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);

            if (type == typeof(Uri))
                return new Uri(str.Replace("\"", string.Empty));
            else
                return JsonConvert.DeserializeObject(str, _serializerSettings);
        }

        public byte[] Serialize<T>(T obj)
        {
            if (typeof(T) == typeof(Uri))
                return System.Text.Encoding.UTF8.GetBytes(obj.ToString());
            else
                return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, _serializerSettings));
        }
    }
}

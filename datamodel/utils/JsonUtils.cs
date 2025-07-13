using System.Linq;
using System.IO;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace datamodel.utils {
    internal static class JsonUtils {
        internal static string JsonPretty(object obj, bool stripQuotes = true) {
            string json = JsonConvert.SerializeObject(obj,
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    Formatting = Formatting.Indented,
                    Converters = [new StringEnumConverter()],
                }).Trim();

            if (stripQuotes)
                json = json.Replace("\"", "");

            return json;
        }

        public static T Deserialize<T>(string json) {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static IEnumerable<JObject> DeserializeMultipleObjects(string json) {
            if (json.TrimStart().First() == '[')
                return DeserializeFromArray(json);
            else
                return DeserializeFromMultipleObjects(json);
        }

        private static IEnumerable<JObject> DeserializeFromArray(string json) {
            List<JObject> jObjects = [];

            using JsonTextReader reader = new(new StringReader(json));
            reader.SupportMultipleContent = true;

            JArray list = (JArray)JToken.ReadFrom(reader);
            foreach (JObject jObject in list)
                jObjects.Add(jObject);

            return jObjects;
        }

        private static IEnumerable<JObject> DeserializeFromMultipleObjects(string json) {
            List<JObject> jObjects = new();
            using JsonTextReader reader = new(new StringReader(json));
            reader.SupportMultipleContent = true;

            while (true) {
                jObjects.Add(JObject.Load(reader));
                if (!reader.Read())
                    break;
            }

            return jObjects;
        }
    }
}

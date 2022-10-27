using System.Text;
using System.IO;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace datamodel.utils {
    internal static class JsonUtils {
        internal static string JsonPretty(object obj, bool stripQuotes = true) {
            string json = JsonConvert.SerializeObject(obj,
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    Formatting = Formatting.Indented,
                    Converters = new List<JsonConverter>() { new StringEnumConverter()},
                }).Trim();

            if (stripQuotes)
                json = json.Replace("\"", "");

            return json;
        }
    }
}
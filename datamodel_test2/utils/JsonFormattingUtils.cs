using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace datamodel.utils {
    internal static class JsonFormattingUtils {

        // Annoyingly, the text captured in _output adds a space in front of
        // every line. This prevents us from simply copy-and-pasting it into
        // the "expected" value - we must remove this space from every line
        // to avoid the toil of removing these spaces manually.
        internal static string DeleteFirstSpace(string text) {
            StringBuilder builder = new StringBuilder();
            using (StringReader reader = new StringReader(text)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;   // Ignore blank lines (at start or end)
                    if (line.StartsWith(" "))
                        builder.AppendLine(line.Substring(1));
                    else 
                        return text.Trim();    // Not all lines start with space - just return original
                }
            }

            return builder.ToString().Trim();
        }

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
using datamodel.schema.tweaks;

using Newtonsoft.Json;

namespace datamodel.schema.source.from_data {
    internal static class FromDataUtils {
        internal static void RemoveColumnLabels(SampleDataSchemaSource source) {
            TempSource data = source._source;
            foreach (Column column in data.AllColumns)
                column.Labels = null;   // Clean up output
        }

        internal static string ToJasonNoQuotes(SampleDataSchemaSource source, bool removeColumnLabels = true) {
            if (removeColumnLabels)
                RemoveColumnLabels(source);

            TempSource data = source._source;
            string json = JsonConvert.SerializeObject(
                data,
                Formatting.Indented,
                new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

            // Quotes are a pain because the make it hard to copy-and-paste results as the 
            // "expected" string
            json = json.Replace("\"", "");

            return json;
        }
    }
}
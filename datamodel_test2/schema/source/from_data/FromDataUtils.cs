using datamodel.schema.tweaks;

using Newtonsoft.Json;

namespace datamodel.schema.source.from_data {
    internal static class FromDataUtils {
        internal static void RemoveColumnLabels(SampleDataSchemaSource source) {
            source._source.RemovePropertyLabels();
        }

        internal static string ToJasonNoQuotes(SampleDataSchemaSource source, bool removeColumnLabels = true) {
            return source._source.ToJasonNoQuotes(removeColumnLabels);
        }

        internal static string ToJasonNoQuotes(SDSS_Element root) {
            string json = JsonConvert.SerializeObject(
                root,
                Formatting.Indented,
                new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

            // Quotes are a pain because the make it hard to copy-and-paste results as the 
            // "expected" string
            json = json.Replace("\"", "");

            return json;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using datamodel.schema.tweaks;

namespace datamodel.schema.source.from_data {
    public class JsonSource : SampleDataSchemaSource {
        public JsonSource(string filename, Options options = null) : this(new string[] { filename }, options) {
            // Do nothing
        }

        public JsonSource(string[] filenames, Options options = null) : base(filenames, options) {
            // Do nothing
        }

        protected override SDSS_Element GetRaw(string json) {
            object root = JsonConvert.DeserializeObject(json);
            return Convert((JToken)root);
        }

        private SDSS_Element Convert(JToken token) {
            if (token is JObject obj) {
                SDSS_Element sdssObj = new SDSS_Element(ElementType.Object);
                foreach (var pair in obj)
                    sdssObj.AddKeyValue(pair.Key, Convert(pair.Value));
                return sdssObj;
            } else if (token is JArray array) {
                return new SDSS_Element(array.Select(x => Convert(x)));
            } else {
                return new SDSS_Element(
                    token.ToString(),
                    token.Type == JTokenType.Null ? null : token.Type.ToString()
                );
            }
        }
    }
}

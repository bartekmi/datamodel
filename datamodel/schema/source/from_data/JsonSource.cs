using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using datamodel.utils;

namespace datamodel.schema.source.from_data {
    public class JsonSource : SampleDataSchemaSource {
        protected override IEnumerable<SDSS_Element> GetRaw(PathAndContent jsonFile) {
            return GetRawInternal(jsonFile);
        }

        internal static IEnumerable<SDSS_Element> GetRawInternal(PathAndContent jsonFile) {      // Exposing for testing
            IEnumerable<JObject> objects = JsonUtils.DeserializeMultipleObjects(jsonFile.Content);
            if (objects.Count() == 0)
                throw new Exception("Could not read JSON records from file: " + jsonFile.Path);

            return objects.Select(x => Convert(x));
        }

        private static SDSS_Element Convert(JToken token) {
            if (token is JObject obj) {
                SDSS_Element sdssObj = new(ElementType.Object);
                foreach (var pair in obj)
                    sdssObj.AddKeyAndValue(pair.Key, Convert(pair.Value));
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

        public override IEnumerable<Parameter> GetParameters() {
            return new List<Parameter>() {
                new ParameterFileOrDir() {
                    Name = PARAM_PATHS,
                    Description = @"Comma-separated list of files and/or directories.
        Only files matching the pattern '*.js?n' will be read.",
                    IsMultiple = true,
                    FilePattern = "*.js?n",
                },
            }.Concat(base.GetParameters());
        }
    }
}

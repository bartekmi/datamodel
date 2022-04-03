using System;
using System.IO;
using System.Linq;

using YamlDotNet.RepresentationModel;

namespace datamodel.schema.source.from_data {
    public class YamlSource : SampleDataSchemaSource {
        public YamlSource(string filename, Options options) : this(new string[] { filename }, options) {
            // Do nothing
        }

        public YamlSource(string[] filenames, Options options) : base(filenames, options) {
            // Do nothing
        }

        public static YamlNode ReadYaml(string yamlString) {
            using (TextReader reader = new StringReader(yamlString)) {
                YamlStream yaml = new YamlStream();

                yaml.Load(reader);
                if (yaml.Documents.Count == 0)
                    return null;

                YamlDocument document = yaml.Documents[0];
                return document.RootNode;
            }
        }

        protected override SDSS_Element GetRaw(string yaml) {
            YamlNode root = ReadYaml(yaml);
            return Convert(root);
        }

        private SDSS_Element Convert(YamlNode token) {
            if (token is YamlMappingNode obj) {
                SDSS_Element sdssObj = new SDSS_Element(ElementType.Object);
                foreach (var pair in obj)
                    sdssObj.AddKeyValue(pair.Key.ToString(), Convert(pair.Value));
                return sdssObj;
            } else if (token is YamlSequenceNode array) {
                return new SDSS_Element(array.Select(x => Convert(x)));
            } else {
                return new SDSS_Element(
                    token.ToString(),
                    DetermineType(token.ToString())
                );
            }
        }

        private string DetermineType(string value) {
            if (bool.TryParse(value, out _))
                return "bool";
            if (long.TryParse(value, out _))
                return "int";
            if (double.TryParse(value, out _))
                return "float";

            return "string";
        }
    }
}

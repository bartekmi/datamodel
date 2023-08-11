using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using YamlDotNet.RepresentationModel;

namespace datamodel.schema.source.from_data {
    public class YamlSource : SampleDataSchemaSource {
        public static YamlNode ReadYaml(PathAndContent yamlFile) {
            using TextReader reader = new StringReader(yamlFile.Content);
            YamlStream yaml = new();

            yaml.Load(reader);
            if (yaml.Documents.Count == 0)
                return null;

            YamlDocument document = yaml.Documents[0];
            return document.RootNode;
        }

        protected override IEnumerable<SDSS_Element> GetRaw(PathAndContent yamlFile) {
            YamlNode root = ReadYaml(yamlFile);
            // Note that YAML does allow multiple records per fail. We just haven't implemented it here yet.
            return new List<SDSS_Element>() { Convert(root) };
        }

        private SDSS_Element Convert(YamlNode token) {
            if (token is YamlMappingNode obj) {
                SDSS_Element sdssObj = new(ElementType.Object);
                foreach (var pair in obj)
                    sdssObj.AddKeyAndValue(pair.Key.ToString(), Convert(pair.Value));
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

        public override IEnumerable<Parameter> GetParameters() {
            return new List<Parameter>() {
                new ParameterFileOrDir() {
                    Name = PARAM_PATHS,
                    Description = @"Comma-separated list of files and/or directories.
        Only files matching the pattern '*.y?ml' will be read.",
                    IsMultiple = true,
                    FilePattern = "*.y?ml",
                },
            }.Concat(base.GetParameters());
        }

    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Linq;

using Newtonsoft.Json;

namespace datamodel.schema.source {

    public class SwaggerSourceOptions {
        // In some schemes (e.g. Kubernetes swagger file), the multi-part model names
        // have repeating components that add nothing interesting. Specify them here.
        public string[] BoringNameComponents;
    }

    public class SwaggerSource : SchemaSource {
        #region Members / Abstract implementations
        private SwgSchema _schema;
        private SwaggerSourceOptions _options;

        private List<Model> _models = new List<Model>();
        private List<Association> _associations = new List<Association>();

        public static SwaggerSource FromUrl(string url, SwaggerSourceOptions options = null) {
            using (WebClient client = new WebClient()) {
                string json = client.DownloadString(url);
                return FromJson(json, options);
            }
        }

        public static SwaggerSource FromFile(string filename, SwaggerSourceOptions options = null) {
            return FromJson(File.ReadAllText(filename), options);
        }

        public static SwaggerSource FromJson(string json, SwaggerSourceOptions options = null) {
            return new SwaggerSource(json, options);
        }

        private SwaggerSource(string json, SwaggerSourceOptions options) {
            // https://stackoverflow.com/questions/22299390/can-not-deserialize-json-containing-ref-keys
            JsonSerializerSettings settings = new JsonSerializerSettings() {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            };

            _schema = JsonConvert.DeserializeObject<SwgSchema>(json, settings);
            _options = options;

            ParseDefinitions();
        }

        public override string GetTitle() {
            return _schema.info?.title;
        }

        public override IEnumerable<Model> GetModels() {
            return _models;
        }

        public override IEnumerable<Association> GetAssociations() {
            return _associations;
        }
        #endregion

        #region Parsing / Extraction
        private void ParseDefinitions() {
            foreach (var definition in _schema.definitions) {
                Model model = ParseDefinition(definition.Key, definition.Value);
                _models.Add(model);
            }
        }

        private Model ParseDefinition(string qualifiedName, SwgDefinition def) {
            ExtractLevelsAndName(qualifiedName, out string[] levels, out string name);

            Model model = new Model() {
                Name = name,
                FullyQualifiedName = qualifiedName,
                Description = def.description,

                Level1 = levels.Length > 0 ? levels[0] : null,
                Level2 = levels.Length > 1 ? levels[1] : null,
                Level3 = levels.Length > 2 ? string.Join(".", levels.Skip(2)) : null,
            };
            model.AllColumns = ParseProperties(model, def.required, def.properties);

            return model;
        }

        private void ExtractLevelsAndName(string qualifiedName, out string[] levels, out string name) {
            IEnumerable<string> pieces = qualifiedName.Split(".");
            if (_options?.BoringNameComponents != null)
                pieces = pieces.Where(x => !_options.BoringNameComponents.Contains(x));
            
            levels = pieces.Take(pieces.Count() - 1).ToArray();
            name = pieces.Last();
        }

        private List<Column> ParseProperties(Model model, IEnumerable<string> required, Dictionary<string, SwgProperty> properties) {
            List<Column> columns = new List<Column>();
            if (properties == null)
                return columns;

            foreach (var item in properties) {
                string name = item.Key;
                SwgProperty prop = item.Value;
                bool isRequired = required == null || required.Contains(name);

                // If type is "array", the actual data type is specified in a nested "items" prop
                bool isArray = prop.type == "array";
                if (isArray) {
                    SwgItems items = prop.items;
                    prop.Reference = items.Reference;
                    prop.format = items.format;
                    prop.type = items.type;
                }

                if (prop.Reference == null)
                    columns.Add(ExtractColumn(model, isRequired, name, isArray, prop));
                else {
                    string reference = prop.Reference;

                    string refPrefix = "#/definitions/";
                    if (!reference.StartsWith(refPrefix)) {
                        Error.Log("Ref {0}.{1}: {2} does not start with {3}",
                            model.FullyQualifiedName, name, reference, refPrefix);
                    } else 
                        reference = reference.Substring(refPrefix.Length);

                    _associations.Add(new Association() {
                        OwnerSide = model.FullyQualifiedName,
                        OwnerMultiplicity = Multiplicity.Aggregation,

                        OtherSide = reference,
                        OtherMultiplicity = isArray ? 
                            Multiplicity.Many : 
                            (isRequired ? Multiplicity.One : Multiplicity.ZeroOrOne),
                        OtherRole = name,

                        Description = prop.description,
                    });
                }
            }

            return columns;
        }

        #region Column Creation
        private Column ExtractColumn(Model model, bool isRequired, string name, bool isArray, SwgProperty prop) {
            string dataType = prop.format == null ? prop.type : prop.format;
            if (prop.Enum != null)
                dataType = "Enum";
                
            if (isArray)
                dataType = "[]" + dataType;

            Column column = new Column(model) {
                Name = name,
                Description = prop.description,
                DataType = dataType,
                CanBeEmpty = !isRequired,
                Enum = ExtractEnum(prop.Enum),
            };

            return column;
        }

        private Enum ExtractEnum(IEnumerable<string> values) {
            if (values == null)
                return null;

            Enum theEnum = new Enum();

            foreach (string value in values)
                theEnum.Add(value, null);

            return theEnum;
        }
        #endregion
        #endregion
    }

    #region Swagger Json Classes
    public class SwgSchema {
        public Dictionary<string, SwgDefinition> definitions;
        public SwgInfo info;
    }

    public class SwgInfo {
        public string title;
        public string version;
    }

    public class SwgDefinition {
        public string description;
        public Dictionary<string, SwgProperty> properties;
        public SwgVersionKind versionKind;
        public string[] required;
        public string type;
    }

    public class SwgVersionKind {
        public string group;
        public string kind;
        public string version;
    }

    public class SwgProperty {
        public string description;
        public string type;
        public string format;
        [JsonProperty("$ref")]
        public string Reference;
        public SwgItems items;
        [JsonProperty("enum")]
        public string[] Enum;

        // Note... This is not general, but specific to Kubernetes
        // Proper solution might be to use some Newtonsoft.JSON functionality
        // to trigger some injectable custom code on parsing and parse non-standard attributes.
        [JsonProperty("x-kubernetes-patch-merge-key")]
        public string PatchMergeKey;
        [JsonProperty("x-kubernetes-patch-strategy")]
        public string PatchMergeStrategy;
    }

    public class SwgItems {
        [JsonProperty("$ref")]
        public string Reference;
        public string type;
        public string format;
    }
    #endregion
}

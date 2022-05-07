using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace datamodel.schema.source {

    public class SwaggerSource : SchemaSource {
        #region Members / Abstract implementations
        private SwgSchema _schema;
        private string[] _boringNameComponents;

        private List<Model> _models = new List<Model>();
        protected List<Association> _associations = new List<Association>();

        public const string PARAM_URL = "url";
        public const string PARAM_FILE = "file";
        public const string PARAM_BORING_NAME_COMPONENTS = "boring-name-components";

        public override void Initialize(Parameters parameters) {
            // https://stackoverflow.com/questions/22299390/can-not-deserialize-json-containing-ref-keys
            JsonSerializerSettings settings = new JsonSerializerSettings() {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            };

            string urlData = parameters.GetUrlContent(PARAM_URL);
            String fileData = parameters.GetFileContent(PARAM_FILE);
            if (!(urlData != null ^ fileData != null))
                throw new Exception(String.Format("Exactly one of these parameters must be set: {0}, {1}",
                    PARAM_URL, PARAM_FILE));

            string json = urlData == null ? fileData : urlData;
            _schema = JsonConvert.DeserializeObject<SwgSchema>(json, settings);
            _boringNameComponents = parameters.GetStrings(PARAM_BORING_NAME_COMPONENTS);

            ParseDefinitions();
        }

        public override IEnumerable<Parameter> GetParameters() {
            return new List<Parameter>() {
                new Parameter() {
                    Name = PARAM_URL,
                    Description = "Download URL for the Swagger schema",
                    Type = ParamType.Url,
                },
                new Parameter() {
                    Name = PARAM_FILE,
                    Description = "File for the Swagger schema",
                    Type = ParamType.File,
                },
                new Parameter() {
                    Name = PARAM_BORING_NAME_COMPONENTS,
                    Description = @"In some schemes (e.g. Kubernetes swagger file), the multi-part model names
        have repeating components that add nothing interesting. Specify them here as comma-separated list.",
                    Type = ParamType.String,
                    IsMultiple = true,
                },
            };
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
            Model model = new Model();
            model.AllColumns = ParseProperties(qualifiedName, def.required, def.properties);

            PopulateModel(model, qualifiedName, def);

            return model;
        }

        protected virtual void PopulateModel(Model model, string qualifiedName, SwgDefinition def) {
            IEnumerable<string> pieces = qualifiedName.Split(".");
            if (_boringNameComponents != null)
                pieces = pieces.Where(x => !_boringNameComponents.Contains(x));

            model.Levels = pieces.Take(pieces.Count() - 1).ToArray();
            model.Name = pieces.Last();
            model.QualifiedName = qualifiedName;
            model.Description = def.description;
        }

        private List<Column> ParseProperties(string modelName, IEnumerable<string> required, Dictionary<string, SwgProperty> properties) {
            List<Column> columns = new List<Column>();
            if (properties == null)
                return columns;

            foreach (var item in properties) {
                string name = item.Key;
                SwgProperty prop = item.Value;
                bool isRequired = required != null && required.Contains(name);

                // If type is "array", the actual data type is specified in a nested "items" prop
                bool isArray = prop.type == "array";
                if (isArray) {
                    SwgItems items = prop.items;
                    prop.Reference = items.Reference;
                    prop.format = items.format;
                    prop.type = items.type;
                }

                if (prop.Reference == null)
                    columns.Add(ExtractColumn(isRequired, name, isArray, prop));
                else {
                    string reference = prop.Reference;

                    string refPrefix = "#/definitions/";
                    if (!reference.StartsWith(refPrefix)) {
                        Error.Log("Ref {0}.{1}: {2} does not start with {3}",
                            modelName, name, reference, refPrefix);
                    } else
                        reference = reference.Substring(refPrefix.Length);

                    _associations.Add(new Association() {
                        OwnerSide = modelName,
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
        private Column ExtractColumn(bool isRequired, string name, bool isArray, SwgProperty prop) {
            Column column = new Column() {
                Name = name,
                Description = prop.description,
                DataType = ComputeType(isArray, prop),
                CanBeEmpty = !isRequired,
                Enum = ExtractEnum(prop.Enum),
            };

            return column;
        }

        // This was arrived largely by reverse-engineering the Kubernetes Swagger file
        // and comparing it with documentation and go source code
        private string ComputeType(bool isArray, SwgProperty prop) {
            SwgAdditionalProperties additionalProp = prop.additionalProperties;
            if (additionalProp != null) {
                bool isApArray = additionalProp.type == "array";
                if (additionalProp.items != null) {
                    additionalProp.type = additionalProp.items.type;
                    additionalProp.Reference = additionalProp.items.Reference;
                }

                string mapType = null;
                if (additionalProp.Reference != null)
                    mapType = additionalProp.Reference.Split('.').Last();
                else
                    mapType = additionalProp.type;

                if (isApArray)
                    mapType = "[]" + mapType;

                return string.Format("map[string] to {0}", mapType);
            }

            string dataType = prop.format == null ? prop.type : prop.format;
            if (prop.Enum != null)
                dataType = "Enum";

            if (isArray)
                dataType = "[]" + dataType;

            return dataType;
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
        public string[] required;
        public string type;
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
        public SwgAdditionalProperties additionalProperties;

        // Note... This is not general, but specific to Kubernetes
        // Proper solution might be to use some Newtonsoft.JSON functionality
        // to trigger some injectable custom code on parsing and parse non-standard attributes.
        [JsonProperty("x-kubernetes-patch-merge-key")]
        public string PatchMergeKey;
        [JsonProperty("x-kubernetes-patch-strategy")]
        public string PatchMergeStrategy;
    }

    public class SwgAdditionalProperties {
        [JsonProperty("$ref")]
        public string Reference;
        public string type;
        public SwgItems items;
    }

    public class SwgItems {
        [JsonProperty("$ref")]
        public string Reference;
        public string type;
        public string format;
    }
    #endregion
}

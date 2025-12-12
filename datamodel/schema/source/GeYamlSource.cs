using System.Collections.Generic;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using System.Linq;

namespace datamodel.schema.source {

    public class GeYamlSource : SchemaSource {
        #region Members / Abstract 
        private readonly List<GeYamlSchema> _schemas = [];

        private readonly List<Model> _models = [];
        protected readonly List<Association> _associations = [];

        public const string PARAM_DIR = "dir";

        public override void Initialize(Parameters parameters) {
            FileOrDir[] fileOrDirs = parameters.GetFileOrDirs(PARAM_DIR);
            IEnumerable<PathAndContent> files = FileOrDir.Combine(fileOrDirs);

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            foreach (PathAndContent pac in files) {
                Console.WriteLine("Processing GE YAML schema file: {0}", pac.Path);
                GeYamlSchema schema = deserializer.Deserialize<GeYamlSchema>(pac.Content);
                _schemas.Add(schema);
            }

            ParseDefinitions();
        }

        public override IEnumerable<Parameter> GetParameters() {
            return [
                new ParameterFileOrDir() {
                    Name = PARAM_DIR,
                    Description = "Directory containing the YAML schema files.",
                    IsMandatory = true,
                    FilePattern = "*.yml",
                    ReadContent = true,
                    IsMultiple = true,
                },
            ];
        }


        public override string GetTitle() {
            return "CC Data Fabric Schema";
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
            foreach (var schema in _schemas) {
                Model model = ParseDefinition(schema);
                _models.Add(model);
            }
        }

        private Model ParseDefinition(GeYamlSchema schema) {
            Model model = new() {
                // Basic model metadata
                Name = schema.name,
                QualifiedName = schema.name,
                Version = schema.version,
                Description = schema.description
            };

            // Parse fields into Properties
            if (schema.model.fields != null)
                ParseFields(model, model.Name, schema.model.fields);

            // Convert metadata.joins into Associations
            if (schema.model!.metadata!.joins != null)
                foreach (var join in schema.model.metadata.joins) {
                    if (join.targetModel == null)
                        continue;

                    if (join.targetModel == model.Name)
                        continue;   // Skip these as they seem to represent static queries on the model to get lists

                    Association assoc = new() {
                        OwnerSide = model.QualifiedName,
                        OtherSide = join.targetModel,
                        Description = join.description,
                    };

                    string rettype = join.returns.type.ToLower();
                    if (rettype == "list") {
                        assoc.OtherMultiplicity = Multiplicity.Many;
                        assoc.OwnerMultiplicity = Multiplicity.One;
                    } else if (rettype == "object") {
                        assoc.OtherMultiplicity = Multiplicity.One;
                        assoc.OwnerMultiplicity = Multiplicity.Many;
                    } else
                        throw new NotImplementedException($"Unhandled join return type '{rettype}' in model '{model.Name}'");

                    // The same associations are sometimes covered twice via Join from both
                    // sids of a 1:n relationship. This ensures we don't end up with duplicates.
                    if (!AssociationExists(assoc))
                        _associations.Add(assoc);
                }

            return model;
        }

        private bool AssociationExists(Association candidate) {
            return _associations.Any(x => x.ReverseSides().IsRoughlyTheSame(candidate));
        }

        private void ParseFields(Model owner, string ownerQualifiedName, Dictionary<string, GeField> fields) {
            foreach (var kvp in fields) {
                string name = kvp.Key;
                GeField field = kvp.Value;
                string type = field.type.ToLower();

                if (type == "object" || type == "array" || type == "list")
                    ParseNestedField(owner, ownerQualifiedName, name, field, type);
                else
                    ParseScalarField(owner, name, field);
            }
        }

        private void ParseNestedField(Model owner, string ownerQualifiedName, string propName, GeField field, string fieldTypeLower) {
            string childQualified = (ownerQualifiedName ?? owner.Name) + "." + propName;

            Model child = new() {
                Name = propName,
                QualifiedName = childQualified,
                Description = field.GetDescription(),
                Version = owner.Version
            };
            _models.Add(child);

            // Recurse to populate child's properties / nested models
            ParseFields(child, childQualified, field.fields);

            // Create association from owner -> child
            Association assoc = new Association() {
                OwnerSide = owner.QualifiedName ?? owner.Name,
                OwnerMultiplicity = Multiplicity.Aggregation,
                OtherSide = child.QualifiedName,
                OtherRole = propName,
            };

            if (assoc.OtherSide == null)
                throw new Exception("Blank OtherSide");

            // Determine other multiplicity
            if (fieldTypeLower == "array" || fieldTypeLower == "list")
                assoc.OtherMultiplicity = Multiplicity.Many;
            else
                assoc.OtherMultiplicity = field.optional ? Multiplicity.ZeroOrOne : Multiplicity.One;

            _associations.Add(assoc);
        }

        private void ParseScalarField(Model model, string propName, GeField field) {
            Property prop = new();
            prop.Name = propName;
            prop.Description = field.GetDescription();
            prop.CanBeEmpty = field.optional;
            prop.DataType = field.type.ToLower();

            if (field?.metadata != null) {
                if (!string.IsNullOrEmpty(field.metadata.resolver))
                    prop.AddLabel("resolver", field.metadata.resolver);
                if (!string.IsNullOrEmpty(field.metadata.example))
                    prop.AddLabel("example", field.metadata.example);
            }

            model.AllProperties.Add(prop);
        }
        #endregion

        #region Model Classes for YAML Schema

        // POCO classes that mirror the YAML schema files (kept simple and `public` like
        // the Swagger example). Field names are lowercase to align with YAML keys.
        public class GeYamlSchema {
            public string name;
            public string version;
            public string description;
            public GeModel model;
        }

        public class GeModel {
            public GeMetadata metadata;
            public Dictionary<string, GeField> fields;
        }

        public class GeMetadata {
            public string resolver;
            public List<GeJoin> joins = [];
            public List<GeKey> keys = [];
        }

        public class GeJoin {
            public string targetModel;
            public string targetField;
            public string sourceField;
            public string resolver;
            public string description;
            public GeReturn returns;
            public string instructions;
        }

        public class GeParam {
            public string name;
            public string type;
            public string description;
        }

        public class GeReturn {
            public string name;
            public string type;
            public string description;
        }

        public class GeKey {
            public string name;
            public string type;
            public string example;
            public List<GeSourcing> sourcing;
        }

        public class GeSourcing {
            public string type;
            public string source;
            public string expression;
            public string description;
        }

        // Generic representation for a field definition. Many fields are simple scalars,
        // but some are `array` or `object` and contain nested `fields`.
        public class GeField {
            public string type;
            public bool optional;
            public string description;
            public string storage;
            public GeFieldMetadata metadata;
            public Dictionary<string, GeField> fields;    // For embedded objects/arrays

            internal string GetDescription() {
                if (metadata != null && !string.IsNullOrEmpty(metadata.description))
                    return metadata.description;
                return description;
            }
        }

        public class GeFieldMetadata {
            public string resolver;
            public string example;
            public string description;
            public List<GeSourcing> sourcing;
        }

        #endregion
    }
}

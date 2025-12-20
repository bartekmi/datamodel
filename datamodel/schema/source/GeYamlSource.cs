using System.Collections.Generic;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using System.Linq;
using System.IO;

namespace datamodel.schema.source {
    // Questions for Makis:
    // 0. General Questions
    //      a) In general, it is not possible to deduce what many of the models actually represent. Recommend
    //          discovery session where models can be better documented.
    //.     b) All enum-style values are strings. Can these be enumerated, or is there no consistency among hospitals. Can such fields be
    //          mapped to FHIR fields which do have defined values? What about case sensitivity?
    //      c) I have found several cases where YAML contains things that the code does not - e.g. Pli.Disposition, Pli.LDAOrders - why?
    //.  
    // 1. The concept of "Schedule" seems to be missing
    // 2. Are hospital personnel entities even used?
    // 3. Is "Facility" and "Department" synonymous - see DivertStatus: GetActiveDivert(departmentId) / facilitySourceId
    // 4. What exactly is FacilitiesLocationMaster? It has no associations and seems to contain bed info (see Hierarchy)
    //      Similar question for UnitsLocationMaster.


    // To run this in Bartek's account:
    // dotnet run ge dir=/Users/250023731/gitlab/docs-site/models/specification
    public class GeYamlSource : SchemaSource {
        #region Members / Abstract 

        // Mapping of "targetField" key name to Entity name
        // Used to create associations from "Joins" where targetModel is same as the model 
        private Dictionary<string, string> _keyToEntity = new() {
            ["patientId"] = "Patient",
            ["mrn"] = "Patient",
            ["patientMRN"] = "Patient",
            ["visitID"] = "PatientVisit",
            ["visitNumber"] = "PatientVisit",
            ["encounterId"] = "PatientVisit",
            ["patientCSN"] = "PatientVisit",
        };

        private readonly List<GeYamlSchema> _schemas = [];
        private readonly List<Model> _models = [];
        private readonly List<Association> _associations = [];
        private string _currentFilename = "";

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
                schema.SourceFilename = Path.GetFileName(pac.Path);
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
            return "CI4Ops Data Fabric Schema";
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
                _currentFilename = schema.SourceFilename;
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
                Description = schema.GetDescription(),
                Levels = [schema.group],
            };
            model.AddLabel("Filename", _currentFilename);

            // Parse keys and fields into Properties
            if (schema.model?.metadata?.keys != null)
                ParseKeys(model, model.Name, schema.model.metadata.keys);
            if (schema.model?.fields != null)
                ParseFields(model, model.Name, schema.model.fields);

            // Convert metadata.joins into Associations
            if (schema.model?.metadata?.joins != null)
                foreach (var join in schema.model.metadata.joins) {
                    if (join.targetModel == null)
                        continue;

                    string ownerSideModelName = model.QualifiedName;
                    if (join.targetModel == model.Name) {
                        if (_keyToEntity.TryGetValue(join.sourceField, out string entityName)) {
                            ownerSideModelName = entityName;
                        } else {
                            Console.WriteLine("WARNING: Skipping self-referencing Join on {0}: {1}. Unknown Source Field: {2}",
                                join.targetModel, join.resolver, join.sourceField);
                            continue;   // Skip these as they seem to represent static queries on the model to get lists
                        }
                    }

                    Association assoc = new() {
                        OwnerSide = ownerSideModelName,
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
            return
                _associations.Any(x => x.IsRoughlyTheSame(candidate)) ||
                _associations.Any(x => x.ReverseSides().IsRoughlyTheSame(candidate));
        }

        private void ParseFields(Model owner, string ownerQualifiedName, Dictionary<string, GeField> fields) {
            int scalarFieldCount = 0;
            Dictionary<string, int> sourceToCount = [];

            foreach (var kvp in fields) {
                string name = kvp.Key;
                GeField field = kvp.Value;
                string type = field.type.ToLower();

                if (type == "object" || type == "array" || type == "list")
                    ParseNestedField(owner, ownerQualifiedName, name, field, type);
                else {
                    scalarFieldCount++;
                    string source = ParseScalarField(owner, name, field);
                    if (source != null) {
                        if (sourceToCount.TryGetValue(source, out int count))
                            sourceToCount[source] = count + 1;
                        else
                            sourceToCount[source] = 1;
                    }
                }
            }

            if (sourceToCount.Count > 0) {
                owner.AddLabel("Sources", string.Join(", ", sourceToCount
                    .Select(x => string.Format("{0}: {1}", x.Key, x.Value))));
            }
        }

        private void ParseKeys(Model model, string name, List<GeKey> keys) {
            foreach (GeKey key in keys) {
                Property prop = new() {
                    Name = key.name,
                    CanBeEmpty = false,
                    DataType = "string",
                };

                prop.AddLabel("Key Type", key.type);

                if (!string.IsNullOrWhiteSpace(key.example))
                    prop.AddLabel("Example", key.example);

                ParseSourcing(prop, key.sourcing);

                model.AllProperties.Add(prop);
            }
        }

        private void ParseNestedField(Model owner, string ownerQualifiedName, string propName, GeField field, string fieldTypeLower) {
            string childQualified = (ownerQualifiedName ?? owner.Name) + "." + propName;

            Model child = new() {
                Name = propName,
                QualifiedName = childQualified,
                Description = field.GetDescription(),
                Version = owner.Version,
                Levels = owner.Levels,
            };
            child.AddLabel("Filename", _currentFilename);
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

        private static string ParseScalarField(Model model, string propName, GeField field) {
            Property prop = new() {
                Name = propName,
                Description = field.GetDescription(),
                CanBeEmpty = field.optional,
                DataType = field.type.ToLower(),
            };

            string source = null;
            if (field?.metadata != null) {
                GeFieldMetadata meta = field.metadata;
                if (!string.IsNullOrWhiteSpace(meta.resolver))
                    prop.AddLabel("Resolver", meta.resolver);

                source = ParseSourcing(prop, meta.sourcing);

                if (!string.IsNullOrWhiteSpace(meta.example))
                    prop.AddLabel("Example", meta.example);
            }

            model.AllProperties.Add(prop);
            return source;
        }

        private static string ParseSourcing(Property prop, List<GeSourcing> sourcingList) {
            GeSourcing source = sourcingList?.FirstOrDefault();
            string sourceTxt = null;

            if (source != null) {
                string[] pieces = [source.type, source.source];
                sourceTxt = string.Join('.', pieces.Where(x => !string.IsNullOrWhiteSpace(x)));
                if (string.IsNullOrWhiteSpace(sourceTxt))
                    sourceTxt = null;
                else
                    prop.AddLabel("Sourcing", sourceTxt);

                if (!string.IsNullOrWhiteSpace(source.expression))
                    prop.AddLabel("Expression", source.expression);

                if (!string.IsNullOrWhiteSpace(source.example))
                    prop.AddLabel("Example", source.example);

                if (string.IsNullOrEmpty(prop.Description) && !string.IsNullOrEmpty(source.description))
                    prop.Description = source.description;

                if (source.type?.ToUpper() == "FHIR" && !string.IsNullOrEmpty(source.source)) {
                    string fhirUrl = string.Format("https://www.hl7.org/fhir/{0}.html", source.source.ToLower());
                    prop.AddUrl("FHIR Link", fhirUrl);
                }
            }
            return sourceTxt;
        }

        #endregion

        #region Model Classes for YAML Schema

        // POCO classes that mirror the YAML schema files (kept simple and `public` like
        // the Swagger example). Field names are lowercase to align with YAML keys.
        public class GeYamlSchema {
            public string name;
            public string version;
            public string group;
            public string description;
            public GeModel model;

            // For my own purpose
            public string SourceFilename;

            public string GetDescription() {
                // Both of these could contain useful info, but either one could be missing.
                string[] descriptions = [description, model?.metadata?.instructions];
                return string.Join("\n\n", descriptions.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
        }

        public class GeModel {
            public GeMetadata metadata;
            public Dictionary<string, GeField> fields;
        }

        public class GeMetadata {
            public string resolver;
            public List<GeJoin> joins = [];
            public string instructions;
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
            public string description;
            public string expression;
            public string example;
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
                // Both of these could contain useful info, but either one could be missing.
                string[] descriptions = [description, metadata?.description, metadata?.sourcing?.FirstOrDefault()?.description];
                return string.Join("\n\n", descriptions.Where(x => !string.IsNullOrWhiteSpace(x)));
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

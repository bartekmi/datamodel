using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using datamodel.schema.source.protobuf.data;

namespace datamodel.schema.source.protobuf {

    public class ProtobufSource : SchemaSource {
        #region Members / Abstract implementations
        private string _title;

        // 1st stage of conversion
        private Dictionary<string, Message> _messages = new Dictionary<string, Message>();
        private Dictionary<string, Enum> _enums = new Dictionary<string, Enum>();

        // 2nd (and final) stage of conversion
        private List<Model> _models = new List<Model>();
        private List<Association> _associations = new List<Association>();

        public const string PARAM_PATHS = "paths";
        public const string PARAM_IMPORT_ROOT = "import-root";
        public const string PARAM_BORING_NAME_COMPONENTS = "boring-name-components";

        public override void Initialize(Parameters parameters) {
            FileOrDir[] fileOrDirs = parameters.GetFileOrDirs(PARAM_PATHS);
            IEnumerable<PathAndContent> files = FileOrDir.Combine(fileOrDirs);
            string firstPath = files.FirstOrDefault()?.Path;
            _title = Path.GetFileName(firstPath);

            string importRoot = parameters.GetString(PARAM_IMPORT_ROOT);

            ProtobufImporter importer = new ProtobufImporter(importRoot);
            FileBundle bundle = importer.ProcessFiles(files);
            InitializeInternal(bundle);
        }

        internal void InitializeInternal(FileBundle bundle) {
            PassOne(bundle);
            PassTwo(bundle);
        }

        public override IEnumerable<Parameter> GetParameters() {
            return new List<Parameter>() {
                new Parameter() {
                    Type = ParamType.String,
                    Name = PARAM_IMPORT_ROOT,
                    Description = "Root directory where imports are looked for",
                    IsMandatory = false,
                    Default = ".",
                },
                new ParameterFileOrDir() {
                    Name = PARAM_PATHS,
                    Description = "The name of the file or directoery which contains the root protobuf file(s). If directory, it is scanned recursively.",
                    IsMandatory = true,
                    IsMultiple = true,
                    FilePattern = "*.proto",
                    // Since we have to read imports anyway, reading some files up-front and some on-the-go
                    // doesn't really make sense. By having a single appraoch, this opens the possibility for a
                    // "fake" file system for unit testing.
                    ReadContent = false,
                },
            };
        }

        public override string GetTitle() {
            return _title;
        }

        public override IEnumerable<Model> GetModels() {
            return _models;
        }

        public override IEnumerable<Association> GetAssociations() {
            return _associations;
        }

        #endregion

        #region Pass One - Build dictionary of all messages and enums

        private void PassOne(FileBundle bundle) {
            foreach (Message message in bundle.AllMessages)
                PassOneMessage(message);
            foreach (EnumDef enumDef in bundle.AllEnumDefs)
                PassOneEnum(enumDef);
        }

        private void PassOneMessage(Message message) {
            _messages[message.QualifiedName()] = message;
            foreach (Message child in message.Messages)
                PassOneMessage(child);
            foreach (EnumDef enumDef in message.EnumDefs)
                PassOneEnum(enumDef);
        }

        private void PassOneEnum(EnumDef enumDef) {
            Enum theEnum = new Enum() {
                Name = enumDef.Name,
                Description = enumDef.Comment,
            };

            foreach (EnumValue value in enumDef.Values)
                theEnum.Add(value.Name, value.Comment);

            _enums[enumDef.Name] = theEnum;
        }
        #endregion

        #region Pass Two - Iterates messages, creates Models, fill in properties, associations

        private void PassTwo(FileBundle bundle) {
            foreach (Message message in _messages.Values)
                PassTwoMessage(message);
            foreach (Service service in bundle.AllServices)
                PassTwoService(service);
        }

        private void PassTwoService(Service service) {
            Model model = new Model() {
                Name = service.Name,
                QualifiedName = service.Name,
                Description = service.Comment,
                // TODO: Try to derive Deprecated
            };

            // For now, we take the easy way and simply reference the models for input and output type.
            // This will no-doubt pollute the diagram, but let's get a sense of it first before
            // trying to make improvements.
            foreach (Rpc rpc in service.Rpcs) {
                model.Methods.Add(new Method() {
                    Name = rpc.Name,
                    Description = rpc.Comment,
                    ParameterTypes = new List<DataType>() {
                        CreateDataType(rpc.InputName),
                    },
                    ReturnType = CreateDataType(rpc.OutputName),
                });
            }

            _models.Add(model);
        }

        private DataType CreateDataType(string name) {
            _enums.TryGetValue(name, out Enum theEnum);
            return new DataType() {
                Name = name,
                Enum = theEnum,
            };
        }

        private void PassTwoMessage(Message message) {
            Model model = new Model() {
                Name = message.Name,
                QualifiedName = message.QualifiedName(),
                Description = message.Comment,
                // TODO: Try to derive Deprecated
            };

            foreach (Field field in message.Fields) {
                if (field is FieldNormal normal)
                    AddFieldNormal(model, normal);
                else if (field is FieldOneOf oneof)
                    AddFieldOneof(model, oneof);
                else if (field is FieldMap map)
                    AddFieldMap(model, map);
                else
                    throw new NotImplementedException("Unexpected field type: " + field.GetType().Name);
            }

            _models.Add(model);
        }


        private void AddFieldNormal(Model model, FieldNormal field) {
            bool isRepeated = field.Modifier == FieldModifier.Repeated;
            AddFieldNormalOrMap(model, field, field.Type, isRepeated, null);
        }

        private void AddFieldNormalOrMap(Model model, Field field, PbType type, bool isRepeated, PbType mapKeyType) {
            _enums.TryGetValue(type.Name, out Enum theEnum);
            Message message = type.ResolveInternalMessage();
            if (message == null)
                _messages.TryGetValue(type.Name, out message);

            if (type.IsAtomic ||        // Atomic types obviously to be represented as Properties
                theEnum != null ||      // Same for enum types
                message == null)        // If for some reason this is NOT a know message, might as well show it as a Prop

                model.AllProperties.Add(new Property() {
                    Name = field.Name,
                    Description = field.Comment,
                    DataType = ComputeType(type, isRepeated, mapKeyType),
                    Enum = theEnum,
                    // TODO: Try to derive Deprecated
                    // TODO: CanBeEmpty can be deduced, but only for proto2, so we need File
                });

            else {
                // Could collect these and spit out some warnings
                // if (!_messages.TryGetValue(field.PbType.Name, out Message message))
                //     throw new Exception("Unknown type for Field " + field);
                
                Association assoc = new Association() {
                    OwnerSide = model.QualifiedName,
                    OwnerMultiplicity = Multiplicity.Aggregation,

                    OtherSide = message.QualifiedName(),
                    OtherMultiplicity = ComputeMultiplicity(isRepeated),
                    OtherRole = field.Name,

                    Description = field.Comment,
                };
                _associations.Add(assoc);
            }

        }

        private string ComputeType(PbType type, bool isRepeated, PbType mapKeyType) {
            if (mapKeyType == null)
                return string.Format("{0}{1}",
                    isRepeated ? "[]" : "",
                    type.Name);
            else
                return string.Format("[{0}]{1}",
                    mapKeyType.Name,
                    type.Name);
        }

        private Multiplicity ComputeMultiplicity(bool isRepeated) {
            // TODO... Can be a bit more precise with protobuf v2
            return isRepeated ?
                Multiplicity.Many :
                Multiplicity.One;
        }

        private void AddFieldOneof(Model model, FieldOneOf oneof) {
            // TODO: If all one-of sub-fields are Entities, consider representing this
            // as a class hierarchy

            foreach (FieldNormal normal in oneof.Fields) {
                // Combine the oneof and normal field comments. A bit redundant, but better
                // than losing the oneof comments entirely.
                normal.Comment = string.Format("One-of Group: {0}\n\n{1}{2}",
                    oneof.Name,
                    string.IsNullOrWhiteSpace(oneof.Comment) ? "" : oneof.Comment + "\n\n",
                    normal.Comment).Trim();

                AddFieldNormal(model, normal);
            }
        }

        private void AddFieldMap(Model model, FieldMap field) {
            AddFieldNormalOrMap(model, field, field.ValueType, true, field.KeyType);
        }
        #endregion
    }
}
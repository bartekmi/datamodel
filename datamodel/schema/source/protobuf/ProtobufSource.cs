using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.source.protobuf.data;
using datamodel.schema.tweaks;

namespace datamodel.schema.source.protobuf {

    public class ProtobufSource : SchemaSource {
        #region Members / Abstract implementations
        private string _title;
        private string _urlPattern;
        private string _importRoot;

        // 1st stage of conversion
        private Dictionary<string, Message> _messages = new Dictionary<string, Message>();
        private Dictionary<string, Enum> _enums = new Dictionary<string, Enum>();

        // 2nd (and final) stage of conversion
        private Dictionary<string, Model> _models = new Dictionary<string, Model>();
        private List<Association> _associations = new List<Association>();

        public const string PARAM_PATHS = "paths";
        public const string PARAM_IMPORT_ROOT = "import-root";
        public const string PARAM_URL_PATTERN = "url-pattern";

        public ProtobufSource() {
            Tweaks.Add(new SimplifyMethodsTweak());
        }

        public override void Initialize(Parameters parameters) {
            FileOrDir[] fileOrDirs = parameters.GetFileOrDirs(PARAM_PATHS);
            IEnumerable<PathAndContent> files = FileOrDir.Combine(fileOrDirs);
            _title = parameters.GetRawText(PARAM_PATHS);
            _urlPattern = parameters.GetString(PARAM_URL_PATTERN);
            _importRoot = parameters.GetString(PARAM_IMPORT_ROOT);

            Console.WriteLine("{0} files found", files.Count());
            ProtobufImporter importer = new ProtobufImporter(_importRoot);
            FileBundle bundle = importer.ProcessFiles(files);

            // Some debug info...
            Console.WriteLine("Protobuf Schema Source...");
            Console.WriteLine("{0} files scanned.", bundle.FileDict.Count());
            Console.WriteLine("{0} messages discovered.", bundle.AllMessages.Count());
            Console.WriteLine("{0} services discovered.", bundle.AllServices.Count());

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
                new Parameter() {
                    Type = ParamType.String,
                    Name = PARAM_URL_PATTERN,
                    Description = @"If specified, must provide a pattern that will evaluate to the URL of the source
code with substitutions for the relative path of the proto file and the line number.
Use '_FILE_' for the file replacement placeholder and _LINE_ for the line replacement
placeholder - e.g. 'http://github.com/my/repo/blob/master/_FILE_#L_LINE+",
                },
            };
        }

        public override string GetTitle() {
            return _title;
        }

        public override IEnumerable<Model> GetModels() {
            return _models.Values;
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

            _enums[enumDef.QualifiedName()] = theEnum;
        }
        #endregion

        #region Pass Two - Iterates messages, creates Models, fill in properties, associations

        private void PassTwo(FileBundle bundle) {
            foreach (Message message in _messages.Values)
                PassTwoMessage(message);
            foreach (Service service in bundle.AllServices)
                PassTwoService(service);
        }

        private string[] ComputeLevels(PbFile file) {
            if (file.Package == null)
                return null;
            return file.Package.Split('.');
        }

        private void MaybeAddUrlLabel(Model model, Base pbBase) {
            if (_urlPattern == null)
                return;

            PbFile file = null;
            if (pbBase is Owner owner)
                file = owner.OwnerFile();
            else if (pbBase is Service service)
                file = service.Owner;
            else
                throw new NotImplementedException("Fill in the other possibility");

            if (!file.Path.StartsWith(_importRoot))
                return;

            string relativePath = file.Path.Substring(_importRoot.Length);
            if (relativePath.StartsWith('/') || relativePath.StartsWith('\\'))
                relativePath = relativePath.Substring(1);

            string url = _urlPattern
                .Replace("_FILE_", relativePath)
                .Replace("_LINE_", pbBase.LineNumber.ToString());

            model.AddUrl("Source Location", url);
        }

        private void PassTwoService(Service service) {
            string suffix = service.Name.ToLower().EndsWith("service") ?
                "" : "Service";

            Model model = new Model() {
                Name = service.Name + suffix,
                QualifiedName = service.QualifiedName + suffix,
                Description = service.Comment,
                Levels = ComputeLevels(service.Owner),
                // TODO: Try to derive Deprecated
            };
            MaybeAddUrlLabel(model, service);

            foreach (Rpc rpc in service.Rpcs) {
                string inputQN = QualifiedName(rpc.InputType);
                string outputQN = QualifiedName(rpc.OutputType);

                model.Methods.Add(new Method() {
                    Name = rpc.Name,
                    Description = rpc.Comment,
                    Inputs = NamedTypeList(inputQN),
                    Outputs = NamedTypeList(outputQN),
                });

                // Experimental
                AddAssociationForService(model, inputQN);
                AddAssociationForService(model, outputQN);
            }

            SafelyAddModel(model, service.Owner.AsFile(), "Service " + service.Name);
        }

        private void AddAssociationForService(Model serviceModel, string otherSideQN) {
            Association assoc = new Association() {
                OwnerSide = serviceModel.QualifiedName,
                OwnerMultiplicity = Multiplicity.Aggregation,
                OtherSide = otherSideQN,
                OtherMultiplicity = Multiplicity.One,
            };
            _associations.Add(assoc);
        }

        private string QualifiedName(PbType type) {
            type.ResolveInternal(out Message message, out _);
            string typeName = message == null ? type.Name : message.QualifiedName();
            return typeName;
        }

        private static List<NamedType> NamedTypeList(string qualifiedName) {
            return [
                new() {
                    Type = new DataType() {
                        Name = qualifiedName,
                    }
                }
            ];
        }

        private void PassTwoMessage(Message message) {
            Model model = new() {
                Name = message.Name,
                QualifiedName = message.QualifiedName(),
                Description = message.Comment,
                Levels = ComputeLevels(message.OwnerFile()),

                // TODO: Try to derive Deprecated
            };
            MaybeAddUrlLabel(model, message);

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

            SafelyAddModel(model, message.OwnerFile(), "Message " + message.Name);
        }


        private void AddFieldNormal(Model model, FieldNormal field) {
            bool isRepeated = field.Modifier == FieldModifier.Repeated;
            AddFieldNormalOrMap(model, field, field.Type, isRepeated, null);
        }

        private void AddFieldNormalOrMap(Model model, Field field, PbType type, bool isRepeated, PbType mapKeyType) {
            type.ResolveInternal(out Message message, out EnumDef enumDef);
            if (message == null)
                _messages.TryGetValue(type.Name, out message);

            string enumName = enumDef == null ? type.Name : enumDef.QualifiedName();
            _enums.TryGetValue(enumName, out Enum theEnum);

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

        private void SafelyAddModel(Model model, PbFile file, string extraInfo) {
            if (_models.ContainsKey(model.QualifiedName)) {
                throw new Exception(string.Format(@"

Duplicate model qualified name: {0}
File: {1}
Package: {2}
Extra Info: {3}

",
                    model.QualifiedName,
                    file.Path,
                    file.Package,
                    extraInfo));
            }
            _models[model.QualifiedName] = model;
        }
        #endregion
    }
}
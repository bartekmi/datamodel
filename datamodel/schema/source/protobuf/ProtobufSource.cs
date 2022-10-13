using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace datamodel.schema.source.protobuf {

    public class ProtobufSource : SchemaSource {
        #region Members / Abstract implementations
        private string[] _boringNameComponents;
        private string _title;

        // 1st stage of conversion
        private Dictionary<string, Message> _messages = new Dictionary<string, Message>();
        private Dictionary<string, Enum> _enums = new Dictionary<string, Enum>();

        // 2nd (and final) stage of conversion
        private List<Model> _models = new List<Model>();
        private List<Association> _associations = new List<Association>();

        public const string PARAM_FILE = "file";
        public const string PARAM_BORING_NAME_COMPONENTS = "boring-name-components";

        public override void Initialize(Parameters parameters) {
            string fileName = parameters.GetRawText(PARAM_FILE);
            string fileData = parameters.GetFileContent(PARAM_FILE);

            InitializeInternal(fileName, fileData);
        }

        internal void InitializeInternal(string fileName, string fileData) {
            _title = Path.GetFileName(fileName);

            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(fileData));
            ProtobufParser parser = new ProtobufParser(tokenizer);
            File pbFile = parser.Parse();

            // TODO: How best to represent Services and Rpc's? Service could be a "class"
            // which holds multiple Rpc's which hold request/response messages, but this
            // could get messy.

            PassOne(pbFile);
            PassTwo();
        }

        public override IEnumerable<Parameter> GetParameters() {
            return new List<Parameter>() {
                new Parameter() {
                    Name = PARAM_FILE,
                    Description = "The name of the file which contains the root protobuf file",
                    Type = ParamType.File,
                    IsMandatory = true,
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

        #region Pass One
        // Pass one build a dictionary of all messages and enums

        private void PassOne(File pbFile) {
            foreach (Message message in pbFile.Messages)
                PassOneMessage(message);
            foreach (EnumDef enumDef in pbFile.EnumDefs)
                PassOneEnum(enumDef);
        }

        private void PassOneMessage(Message message) {
            _messages[message.Name] = message;
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

        #region Pass Two
        // Pass two iterates dictionary of messages, creates Models, fills in properties
        // and associations.

        private void PassTwo() {
            foreach (Message message in _messages.Values)
                PassTwoMessage(message);
        }

        private void PassTwoMessage(Message message) {
            Model model = new Model() {
                Name = message.Name,
                QualifiedName = message.Name,
                Description = message.Comment,
                // TODO: This can be read from options
                // Deprecated = ...
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
            _enums.TryGetValue(field.Type.Name, out Enum theEnum);

            if (field.Type.IsAtomic || theEnum != null)
                model.AllColumns.Add(new Column() {
                    Name = field.Name,
                    Description = field.Comment,
                    DataType = ComputeType(field),
                    Enum = theEnum,
                    // TODO...
                    // Deprecated = Extract from Options
                    // CanBeEmpty - This can be deduced, but only for proto2, so we need File
                });

            else {
                // Could collect these and spit out some warnings
                // if (!_messages.TryGetValue(field.Type.Name, out Message message))
                //     throw new Exception("Unknown type for Field " + field);
                
                Association assoc = new Association() {
                    OwnerSide = model.QualifiedName,
                    OwnerMultiplicity = Multiplicity.Aggregation,

                    OtherSide = field.Type.Name,
                    OtherMultiplicity = ComputeMultiplicity(field),
                    OtherRole = field.Name,

                    Description = field.Comment,
                };
                _associations.Add(assoc);
            }

        }

        private Multiplicity ComputeMultiplicity(FieldNormal field) {
            // TODO... Can be a bit more precise with protobuf v2
            return field.Modifier == FieldModifier.Repeated ?
                Multiplicity.Many :
                Multiplicity.One;
        }

        private string ComputeType(FieldNormal field) {
            return string.Format("{0}{1}",
                field.Modifier == FieldModifier.Repeated ? "[]" : "",
                field.Type.Name);
        }

        private void AddFieldOneof(Model model, FieldOneOf oneof) {

        }

        private void AddFieldMap(Model model, FieldMap map) {

        }
        #endregion
    }
}
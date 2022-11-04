using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf.data {

    public class PbFile : Base, Owner {
        private Dictionary<string,Message> _messageByQN;

        public string Path { get; set; }
        public string Package { get; set; }
        public string Syntax { get; set; }

        public List<Import> Imports { get; } = new List<Import>();
        public List<Service> Services { get; }=  new List<Service>();
        public List<Message> Messages { get; } = new List<Message>();
        public List<EnumDef> EnumDefs { get; } = new List<EnumDef>();
        public List<Extend> Extends { get; } = new List<Extend>();      // Protobuf 2 only

        // For the sake of JSON serialization
        public bool ShouldSerializeImports() { return Imports.Count > 0; }
        public bool ShouldSerializeServices() { return Services.Count > 0; }
        public bool ShouldSerializeMessages() { return Messages.Count > 0; }
        public bool ShouldSerializeEnumDefs() { return EnumDefs.Count > 0; }
        public bool ShouldSerializeExtends() { return Extends.Count > 0; }

        // Owner interface
        [JsonIgnore]
        public string Name { get => Package; }

        public IEnumerable<Message> AllMessages() {
            List<Message> messages = new List<Message>();

            messages.AddRange(Messages);
            foreach (Message message in Messages)
                AddNestedMessages(messages, message);

            return messages;
        }

        private void AddNestedMessages(List<Message> messages, Message message) {
            messages.AddRange(message.Messages);
            foreach (Message nested in message.Messages) {
                AddNestedMessages(messages, nested);
            }
        }

        public IEnumerable<PbType> AllTypes() {
            IEnumerable<PbType> typesFromMessage = AllMessages()
                .SelectMany(x => x.Fields)
                .SelectMany(x => x.UsedTypes());

            List<PbType> typesFromServices = new List<PbType>();
            foreach (Rpc rpc in Services.SelectMany(x => x.Rpcs)) {
                typesFromServices.Add(rpc.InputType);
                typesFromServices.Add(rpc.OutputType);
            }

            return typesFromMessage
                .Concat(typesFromServices)
                .Distinct();
        }

        public Message TryGetMessage(string qualifiedName) {
            if (_messageByQN == null)
                _messageByQN = AllMessages().ToDictionary(x => x.QualifiedName());

            _messageByQN.TryGetValue(qualifiedName, out Message message);
            return message;
        }

        public IEnumerable<EnumDef> AllEnumDefs() {
            List<EnumDef> enums = new List<EnumDef>();

            enums.AddRange(EnumDefs);
            foreach (Message message in Messages)
                AddNestedEnumDefs(enums, message);
                
            return enums;
        }

        private void AddNestedEnumDefs(List<EnumDef> enums, Message message) {
            enums.AddRange(message.EnumDefs);
            foreach (Message nested in message.Messages) 
                AddNestedEnumDefs(enums, nested);
        }

        // Useful to remove clutter when converting to JSON for debugging
        internal void RemoveComments() {
            Comment = null;

            foreach (var item in AllMessages()) item.Comment = null;
            foreach (var item in AllMessages().SelectMany(x => x.Fields)) item.Comment = null;
            foreach (var item in AllEnumDefs()) item.Comment = null;
            foreach (var item in AllEnumDefs()) item.Comment = null;
            foreach (var item in Services) item.Comment = null;
        }

        public override string ToString() {
            return Package;
        }
    }
}

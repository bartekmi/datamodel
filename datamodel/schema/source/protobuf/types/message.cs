using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf {
    public class Message : Base, Owner, Owned {
        public string Name { get; set; }
        public List<Field> Fields { get; } = new List<Field>();
        public List<Message> Messages { get; } = new List<Message>();
        public List<EnumDef> EnumDefs { get; } = new List<EnumDef>();
        public List<Extend> Extends { get; } = new List<Extend>();      // Protobuf 2 only

        // For the sake of JSON serialization
        public bool ShouldSerializeFields() { return Fields.Count > 0; }
        public bool ShouldSerializeMessages() { return Messages.Count > 0; }
        public bool ShouldSerializeEnumDefs() { return EnumDefs.Count > 0; }
        public bool ShouldSerializeExtends() { return Extends.Count > 0; }

        // Owner interface
        public bool IsFile => false;

        // Owned interface
        [JsonIgnore]
        public Owner Owner { get; set; }

        public override string ToString() {
            return Name;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf {
    public class FieldGroup : Field {
        public FieldModifier Modifier { get; set; }
        public List<Field> Fields { get; } = new List<Field>();
        public int Number { get; set; }

        public FieldGroup(Message owner) : base(owner){}

        public override IEnumerable<Type> UsedTypes() {
            return Fields.SelectMany(x => UsedTypes());
        }
    }

    // TODO: Extensions are important, but it is not yet clear to me how best to represent them visually.
    // The Protobuf2 language guide specifically wans against confusing extensions with inheritance.
    // And yet inheritance could be implemented with extensions if each "child" class used a spacified
    // range of extensions. Of course, this is wrought with danger, since two child classes could 
    // accidentally use the same reserved Number for a field.

    // One simple solution would be to simply add the "extended" fields to the original Model/Message.
    // This is kind-of how things were intended.
    public class Extend : Base {
        public string MessageType { get; set; }
        public List<Field> Fields { get; } = new List<Field>();

        // Since we may need to accumulate properties of a Message such
        // as nested message or enum definitions, we need this dummy
        // Message. Later, it could be merged with the "parent" message
        public Message Message { get; } = new Message();
    }
}

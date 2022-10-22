using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf {
    public abstract class Field : Base {
        // Return list of all types used by this field
        public abstract IEnumerable<Type> UsedTypes();
        public string Name { get; set; }

        // Owned interface
        [JsonIgnore]
        public Message Owner { get; set; }
        [JsonIgnore]
        public File File => Owner.OwnerFile();

        public Field(Message owner) {
            Owner = owner;
        }

        public override string ToString() {
            return Name;
        }
    }

    public enum FieldModifier {
        None,
        Required,   // Protobuf 2 only
        Optional,   // Protobuf 2, but can use in 3
        Repeated,
    }
    public class FieldNormal : Field {
        public FieldModifier Modifier { get; set; }
        public Type Type { get; set; }
        public int Number { get; set; }

        public FieldNormal(Message owner) : base(owner){}

        public override IEnumerable<Type> UsedTypes() {
            return new Type[] { Type };
        }
    }

    public class FieldOneOf : Field {
        public List<FieldNormal> Fields { get; } = new List<FieldNormal>();

        public FieldOneOf(Message owner) : base(owner){}

        public override IEnumerable<Type> UsedTypes() {
            return Fields.Select(x => x.Type);
        }
    }

    public class FieldMap : Field {
        public Type KeyType { get; set; }
        public Type ValueType { get; set; }
        public int Number { get; set; }

        public FieldMap(Message owner) : base(owner){}

        public override IEnumerable<Type> UsedTypes() {
            return new Type[] { KeyType, ValueType };
        }
    }
}

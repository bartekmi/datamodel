using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf {
    public class Type {
        static readonly string[] ATOMIC_TYPES = new string[] {
            "double" , "float" , "int32" , "int64" , "uint32" , "uint64"
            , "sint32" , "sint64" , "fixed32" , "fixed64" , "sfixed32" , "sfixed64"
            , "bool" , "string" , "bytes"
        };

        public string Name { get; set; }
        [JsonIgnore]
        public Field OwnerField { get; private set; }
        [JsonIgnore]
        public File OwnerFile => OwnerField.File;

        // Derived
        [JsonIgnore]
        public bool IsAtomic { get => ATOMIC_TYPES.Any(x => x == Name ); } 
        [JsonIgnore]
        public bool IsImported { get => Name.Contains('.'); }
        [JsonIgnore]
        public string QualifiedName {
            get {
                if (IsImported || IsAtomic)
                    return Name;

                return string.Format("{0}.{1}", OwnerFile.Package, Name);
            }
        }

        public Type(Field ownerField, string name) {
            OwnerField = ownerField;
            Name = name;
        }

        public override string ToString() {
            return Name;
        }

        public override int GetHashCode() {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj.GetType() != typeof(Type))
                return false;
            return ((Type)obj).Name == Name;
        }
    }
}

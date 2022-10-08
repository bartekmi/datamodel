using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf {
    public enum ImportType {
        None,
        Weak,
        Public,
    }
    public class File {
        public string Path;
        public string Package;
        public string Syntax;
        public ImportType ImportType;

        public List<File> Imports = new List<File>();
        public List<Service> Services = new List<Service>();
        public List<Message> Messages = new List<Message>();
        public List<TypeEnum> EnumTypes = new List<TypeEnum>();

        // For the sake of JSON serialization
        public bool ShouldSerializeImportType() { return ImportType != ImportType.None; }

        public bool ShouldSerializeImports() { return Imports.Count > 0; }
        public bool ShouldSerializeServices() { return Services.Count > 0; }
        public bool ShouldSerializeMessages() { return Messages.Count > 0; }
        public bool ShouldSerializeEnumTypes() { return EnumTypes.Count > 0; }
    }

    public class Service {
        public string Name;
        public List<Rpc> Rpcs = new List<Rpc>(); 
    }

    public class Rpc {
        public string Name;
        public string InputName;
        public string OutputName;

        public Message Input;
        public Message Output;
    }

    public class Message {
        public string Name;
        public List<Field> Fields = new List<Field>();
        public List<Message> Messages = new List<Message>();
        public List<TypeEnum> EnumTypes = new List<TypeEnum>();

        // For the sake of JSON serialization
        public bool ShouldSerializeMessages() { return Messages.Count > 0; }
        public bool ShouldSerializeEnumTypes() { return EnumTypes.Count > 0; }

    }

    public abstract class Field {
        public string Name;
    }

    public enum FieldModifier {
        None,
        Optional,   // No longer applicable in Protobuf 3
        Repeated,
    }
    public class FieldNormal : Field {
        public FieldModifier Modifier;
        public Type Type;
        public int Number;

        public bool ShouldSerializeModifier() { return Modifier != FieldModifier.None; }
    }

    public class FieldOneOf : Field {
        public List<FieldNormal> Fields = new List<FieldNormal>();
    }

    public class FieldMap : Field {
        public Type KeyType;
        public Type ValueType;
        public int Number;
    }

    public class Type {
        static readonly string[] ATOMIC_TYPES = new string[] {
            "double" , "float" , "int32" , "int64" , "uint32" , "uint64"
            , "sint32" , "sint64" , "fixed32" , "fixed64" , "sfixed32" , "sfixed64"
            , "bool" , "string" , "bytes"
        };

        public string Name;
        public TypeEnum EnumType;
        public Message MessageType;

        // Derived
        [JsonIgnore]
        public bool IsAtomic { get { return ATOMIC_TYPES.Any(x => x == Name ); } }

        public Type(string name) {
            Name = name;
        }
    }

    public class TypeEnum {
        public string Name;
        public List<EnumValue> Values = new List<EnumValue>();
    }

    public class EnumValue {
        public string Name;
        public int Number;
    }
}
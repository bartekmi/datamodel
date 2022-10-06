using System.Collections.Generic;

namespace datamodel.schema.source.protobuf {
    internal enum ImportType {
        None,
        Weak,
        Public,
    }
    internal class File {
        internal string Path;
        internal string Package;
        internal string Syntax;
        internal ImportType ImportType;

        internal List<File> Imports = new List<File>();
        internal List<Service> Services = new List<Service>();
        internal List<Message> Messages = new List<Message>();
        internal List<TypeEnum> EnumTypes = new List<TypeEnum>();
    }

    internal class Service {
        internal string Name;
        internal List<Rpc> Rpcs = new List<Rpc>(); 
    }

    internal class Rpc {
        internal string Name;
        internal string InputName;
        internal string OutputName;

        internal Message Input;
        internal Message Output;
    }

    internal class Message {
        internal string Name;
        internal List<Field> Fields = new List<Field>();
        internal List<Message> Messages = new List<Message>();
        internal List<TypeEnum> EnumTypes = new List<TypeEnum>();
    }

    internal abstract class Field {
        internal string Name;
    }

    internal enum FieldModifier {
        None,
        Optional,   // No longer applicable in Protobuf 3
        Repeated,
    }
    internal class FieldNormal : Field {
        internal bool IsRepeated;
        internal FieldModifier Modifier;
        internal Type Type;
        internal int Number;
    }

    internal class FieldOneOf : Field {
        internal List<FieldNormal> Fields = new List<FieldNormal>();
    }

    internal class FieldMap : Field {
        internal Type KeyType;
        internal Type ValueType;
        internal int Number;
    }

    internal class Type {
        internal string Name;
        internal TypeEnum EnumType;
        internal Message MessageType;

        internal Type(string name) {
            Name = name;
        }
    }

    internal class TypeEnum {
        internal string Name;
        internal List<EnumValue> Values = new List<EnumValue>();
    }

    internal class EnumValue {
        internal string Name;
        internal int Number;
    }
}
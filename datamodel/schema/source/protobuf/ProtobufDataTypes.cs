using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf {

    public class Base {
        public string Comment { get; set; }
        public bool ShouldSerializeComment() { return !string.IsNullOrWhiteSpace(Comment); }
    }

    public interface Owner {
        bool IsFile();
    }

    public interface Owned {
        Owner Owner { get; }
        string Name { get; }
    }

    public static class OwnedExtensions {
        public static string FullyQualifiedName(this Owned owned) {
            List<string> components = new List<string>();
            while (true) {
                components.Add(owned.Name);
                if (owned.Owner.IsFile())
                    break;
                owned = (Owned)owned.Owner;
            } 

            components.Reverse();
            return string.Join(".", components);
        }
    }

    public enum ImportType {
        None,
        Weak,
        Public,
    }
    public class File : Base, Owner {
        public string Path { get; set; }
        public string Package { get; set; }
        public string Syntax { get; set; }
        public ImportType ImportType { get; set; }

        public List<File> Imports { get; } = new List<File>();
        public List<Service> Services { get; }=  new List<Service>();
        public List<Message> Messages { get; } = new List<Message>();
        public List<EnumDef> EnumDefs { get; } = new List<EnumDef>();

        // For the sake of JSON serialization
        public bool ShouldSerializeImports() { return Imports.Count > 0; }
        public bool ShouldSerializeServices() { return Services.Count > 0; }
        public bool ShouldSerializeMessages() { return Messages.Count > 0; }
        public bool ShouldSerializeEnumDefs() { return EnumDefs.Count > 0; }

        // Owner interface
        public bool IsFile() { return true; }
    }

    public class Service : Base {
        public string Name { get; set; }
        public List<Rpc> Rpcs { get; } = new List<Rpc>(); 
    }

    public class Rpc : Base {
        public string Name { get; set; }
        public string InputName { get; set; }
        public bool IsInputStream { get; set; }

        public string OutputName { get; set; }
        public bool IsOutputStream { get; set; }

        public Message Input;
        public Message Output;
    }

    public class Message : Base, Owner, Owned {
        public string Name { get; set; }
        public List<Field> Fields { get; } = new List<Field>();
        public List<Message> Messages { get; } = new List<Message>();
        public List<EnumDef> EnumDefs { get; } = new List<EnumDef>();

        // For the sake of JSON serialization
        public bool ShouldSerializeMessages() { return Messages.Count > 0; }
        public bool ShouldSerializeEnumDefs() { return EnumDefs.Count > 0; }

        // Owner interface
        public bool IsFile() { return false; }

        [JsonIgnore]
        public Owner Owner { get; set; }
    }

    public abstract class Field : Base {
        public string Name { get; set; }
    }

    public enum FieldModifier {
        None,
        Optional,   // No longer applicable in Protobuf 3
        Repeated,
    }
    public class FieldNormal : Field {
        public FieldModifier Modifier { get; set; }
        public Type Type { get; set; }
        public int Number { get; set; }
    }

    public class FieldOneOf : Field {
        public List<FieldNormal> Fields { get; } = new List<FieldNormal>();
    }

    public class FieldMap : Field {
        public Type KeyType { get; set; }
        public Type ValueType { get; set; }
        public int Number { get; set; }
    }

    public class Type {
        static readonly string[] ATOMIC_TYPES = new string[] {
            "double" , "float" , "int32" , "int64" , "uint32" , "uint64"
            , "sint32" , "sint64" , "fixed32" , "fixed64" , "sfixed32" , "sfixed64"
            , "bool" , "string" , "bytes"
        };

        public string Name { get; set; }
        public EnumDef EnumType { get; set; }
        public Message MessageType { get; set; }

        // Derived
        [JsonIgnore]
        public bool IsAtomic { get { return ATOMIC_TYPES.Any(x => x == Name ); } }

        public Type(string name) {
            Name = name;
        }
    }

    public class EnumDef : Base, Owned {
        public string Name { get; set; }
        public List<EnumValue> Values { get; } = new List<EnumValue>();

        [JsonIgnore]
        public Owner Owner { get; set; }
    }

    public class EnumValue : Base {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    #region Specific to Protobuf 2
    public class FieldGroup : Field {
       public FieldModifier Modifier { get; set; }
       public List<Field> Fields { get; } = new List<Field>();
       public int Number { get; set; }
    }

    // TODO: Extensions are important, but it is not yet clear to me how best to represent them visually.
    // The Protobuf2 language guide specifically wans against confusing extensions with inheritance.
    // And yet inheritance could be implemented with extensions if each "child" class used a spacified
    // range of extensions. Of course, this is wrought with danger, since two child classes could 
    // accidentally use the same reserved Number for a field.

    // One simple solution would be to simply add the "extended" fields to the original Model/Message.
    // This is kind-of how things were intended.

   #endregion
}
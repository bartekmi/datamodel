using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf {

    public class Base {
        public string Comment { get; set; }
        public bool ShouldSerializeComment() { return !string.IsNullOrWhiteSpace(Comment); }

        // Though not strictly part of the parse results, this is a convenience flag
        // to indicate whether this entity should be included in the ultimate
        // results.
        // This is important to distinguish imported entities that are needed by
        // Messages from Files we want vs. other redundant entities.
        [JsonIgnore]
        public bool IncludeInResults { get; set; }
    }

    public interface Owner {
        bool IsFile { get; }
    }
    public static class OwnerExtensions {
        public static File OwnerFile(this Owner owner) {
            while (!owner.IsFile)
                owner = ((Owned)owner).Owner;

            return (File)owner;
        }
    }

    public interface Owned {
        Owner Owner { get; }
        string Name { get; }
    }

    public static class OwnedExtensions {
        public static string QualifiedName(this Owned owned) {
            List<string> components = new List<string>();
            while (true) {
                components.Add(owned.Name);
                if (owned.Owner.IsFile) {
                    components.Add(((File)owned.Owner).Package);
                    break;
                }
                owned = (Owned)owned.Owner;
            } 

            components.Reverse();
            return string.Join(".", components);
        }
    }
    public class File : Base, Owner {
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
        public bool IsFile => true;

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

        public IEnumerable<Type> AllTypes() {
            return AllMessages()
                .SelectMany(x => x.Fields)
                .SelectMany(x => x.UsedTypes());
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

    public enum ImportType {
        None,
        Weak,
        Public,
    }
    public class Import {
        public string ImportPath { get; set; }
        public ImportType ImportType { get; set; }

        public override string ToString() {
            return ImportPath;
        }
    }

    public class Service : Base {
        public string Name { get; set; }
        public List<Rpc> Rpcs { get; } = new List<Rpc>(); 

        public override string ToString() {
            return Name;
        }
    }

    public class Rpc : Base {
        public string Name { get; set; }
        public string InputName { get; set; }
        public bool IsInputStream { get; set; }

        public string OutputName { get; set; }
        public bool IsOutputStream { get; set; }

        public override string ToString() {
            return Name;
        }
    }

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

    public class EnumDef : Base, Owned {
        public string Name { get; set; }
        public List<EnumValue> Values { get; } = new List<EnumValue>();

        [JsonIgnore]
        public Owner Owner { get; set; }

        public override string ToString() {
            return Name;
        }
    }

    public class EnumValue : Base {
        public string Name { get; set; }
        public int Number { get; set; }

        public override string ToString() {
            return Name;
        }
    }

    #region Specific to Protobuf 2
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

   #endregion
}
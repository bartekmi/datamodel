using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf.data {
    public class PbType {
        static readonly string[] ATOMIC_TYPES = new string[] {
            "double" , "float" , "int32" , "int64" , "uint32" , "uint64"
            , "sint32" , "sint64" , "fixed32" , "fixed64" , "sfixed32" , "sfixed64"
            , "bool" , "string" , "bytes"
        };

        public string Name { get; set; }
        [JsonIgnore]
        public Field OwnerField { get; private set; }

        // Derived
        [JsonIgnore]
        public bool IsAtomic { get => ATOMIC_TYPES.Any(x => x == Name ); } 
        [JsonIgnore]
        public bool IsImported { 
            get => Name.Contains('.') && ResolveInternalMessage() == null; 
        }
        [JsonIgnore]
        public PbFile OwnerFile => OwnerField.File;
        [JsonIgnore]
        public Message OwnerMessage => OwnerField.Owner;

        public PbType(Field ownerField, string name) {
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
            if (obj.GetType() != typeof(PbType))
                return false;
            return ((PbType)obj).Name == Name;
        }

        private Message _message;   // Cache for performance
        private bool _messageChecked;
        public Message ResolveInternalMessage() {
            if (_messageChecked)
                return _message;

            string[] pieces = Name.Split('.');
            string first = pieces[0];

            // Phase 1: Travel *UP* the chain of owners, trying to find a name
            // match of first piece either in name of current message or in 
            // immediate children (of Message or PbFile).
            Owner owner = OwnerMessage;
            Message baseMessage = null;

            do {
                // Check self
                if (owner.IsMessage() && owner.AsMessage().Name == first) {
                    baseMessage = owner.AsMessage();
                    break;
                }

                // Check children at this level
                foreach (Message message in owner.Messages)
                    if (message.Name == first) {
                        baseMessage = message;
                        break;
                    }

                // Did not succeed at this level - move up
                owner = ((Owned)owner).Owner;
            } while (!owner.IsFile());

            // Phase 2: If base message found, and other pieces exist, travel *DOWN*
            // nested messages
            if (baseMessage != null) {
                foreach (string piece in pieces.Skip(1))
                    baseMessage = baseMessage.Messages.Single(x => x.Name == piece);
            }

            _message = baseMessage;
            _messageChecked = true;

            return _message;
        }
    }
}

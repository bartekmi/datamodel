using System;
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
        public Message OwnerMessage { get; private set; }
        [JsonIgnore]
        public PbFile OwnerFile { get; private set; }

        // Derived
        [JsonIgnore]
        public bool IsAtomic { get => ATOMIC_TYPES.Any(x => x == Name ); } 
        [JsonIgnore]
        public bool IsImported { 
            get {
                ResolveInternal(out Message message, out EnumDef _);
                return Name.Contains('.') && message == null; 
            } 
        }

        public PbType(Field ownerField, string name) {
            OwnerMessage = ownerField.Owner;
            OwnerFile = OwnerMessage.OwnerFile();
            
            Name = name;
        }

        public PbType(PbFile ownerFile, string name) {
            OwnerFile = ownerFile;
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

        private Message _resolvedMessage;   // Cache for performance
        private EnumDef _resolvedEnumDef;   // Cache for performance
        private bool _resolvedChecked;
        public void ResolveInternal(out Message messageOut, out EnumDef enumDefOut) {
            if (_resolvedChecked) {
                messageOut = _resolvedMessage;
                enumDefOut = _resolvedEnumDef;
                return;
            }

            string[] pieces = Name.Split('.', StringSplitOptions.RemoveEmptyEntries);

            messageOut = null;
            enumDefOut = null;

            Owner baseOwner;
            string[] remainingPieces;

            if (Name.StartsWith(".")) {
                remainingPieces = pieces;
                baseOwner = OwnerFile;
            } else {
                string first = pieces[0];
                remainingPieces = pieces.Skip(1).ToArray();
                baseOwner = ResolveInternalPhaseOne(first, out enumDefOut);
            }

            if (baseOwner != null)
                ResolveInternalPhaseTwo(baseOwner, remainingPieces, out messageOut, out enumDefOut);

            _resolvedMessage = messageOut;
            _resolvedEnumDef = enumDefOut;
            _resolvedChecked = true;
        }

        // Phase 1: Travel *UP* the chain of owners, trying to find a name
        // match of first piece either in name of current message or in 
        // immediate children (of Message or PbFile).
        private Owner ResolveInternalPhaseOne(string first, out EnumDef enumDefOut) {
            enumDefOut = null;
            Owner owner = OwnerMessage == null ? OwnerFile : OwnerMessage;
            Owner baseOwner = null;

            while (true) {
                // Check self
                if (owner.Name == first) {
                    baseOwner = owner;
                    break;
                }

                // Check children at this level
                Message message = owner.Messages.FirstOrDefault(x => x.Name == first);
                if (message != null) {
                    baseOwner = message;
                    break;
                }
                enumDefOut = owner.EnumDefs.FirstOrDefault(x => x.Name == first);
                if (enumDefOut != null) {
                    // There is a pathological case where we've found an enum, but there are other "pieces";
                    // ignoring this.
                    return null;
                }

                // If we've reached the File without success, we give up
                if (owner.IsFile())
                    break;

                // Did not succeed at this level - move up
                owner = ((Owned)owner).Owner;
            }

            return baseOwner;
        }

        // Phase 2: If base message found, and other pieces exist, travel *DOWN*
        // (to the leafs of the tree nested messages).
        private void ResolveInternalPhaseTwo(
            Owner baseOwner, 
            IEnumerable<string> remainingPieces, 
            out Message messageOut,
            out EnumDef enumDefOut) {

            messageOut = null;
            enumDefOut = null;

            foreach (string piece in remainingPieces) {
                Owner child = baseOwner.Messages.FirstOrDefault(x => x.Name == piece);
                if (child == null) {
                    enumDefOut = baseOwner.EnumDefs.FirstOrDefault(x => x.Name == piece);
                    if (enumDefOut != null) {
                        // There is a pathological case where we've found an enuml, but there are other "pieces";
                        // ignoring this.
                        return;
                    }
                }

                // This should never happen in a correctly formed proto. This means that we found a base owner,
                // but the pieces did not match going back DOWN the chain.
                if (child == null)
                    return;

                baseOwner = child;
            }

            messageOut = baseOwner.AsMessage();
        }

        public Message ResolveExternalMessage(PbFile file) {
            string[] pieces = Name.Split('.');
            string first = pieces.First();
            if (file.Package != first)
                return null;

            Owner owner = file;
            foreach (string piece in pieces.Skip(1)) {
                owner = owner.Messages.FirstOrDefault(x => x.Name == piece);
                if (owner == null)
                    break;
            }
            
            return owner.AsMessage();
        }

        public Message ResolveMessage(PbFile file) {
            if (OwnerFile == file) {
                ResolveInternal(out Message message, out EnumDef _);
                return message;
            }

            return ResolveExternalMessage(file);
        }
    }
}

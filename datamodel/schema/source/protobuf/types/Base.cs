using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf.data {
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
        List<Message> Messages { get; }
        List<EnumDef> EnumDefs { get; }
        string Name { get; }
    }
    public static class OwnerExtensions {
        public static PbFile OwnerFile(this Owner owner) {
            while (!owner.IsFile())
                owner = owner.Owner();

            return (PbFile)owner;
        }
        public static bool IsMessage(this Owner owner) {
            return !owner.IsFile();
        }
        public static Message AsMessage(this Owner owner) {
            return owner as Message;
        }
        public static bool IsFile(this Owner owner) {
            return owner is PbFile;
        }
        public static PbFile AsFile(this Owner owner) {
            return owner as PbFile;
        }
        public static Owner Owner(this Owner owner) {
            if (owner.IsFile())
                return null;
            return ((Owned)owner).Owner;
        }
    }

    public interface Owned {
        Owner Owner { get; }
        string Name { get; }
    }

    public static class OwnedExtensions {
        public static string QualifiedName(this Owned owned) {
            List<string> components = new List<string>();
            components.Add(owned.Name);
            Owner owner = owned.Owner;

            while (owner != null) {
                components.Add(owner.Name);
                owner = owner.Owner();
            } 

            IEnumerable<string> reversed = components
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Reverse();
                
            return string.Join(".", reversed);
        }
    }
}

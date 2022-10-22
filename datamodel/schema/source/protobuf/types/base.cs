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
        [JsonIgnore]
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
}

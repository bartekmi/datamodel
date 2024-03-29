using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf.data {
    public class Service : Base {
        public string Name { get; set; }
        public List<Rpc> Rpcs { get; } = new List<Rpc>(); 
        [JsonIgnore]
        public PbFile Owner { get; private set; }

        // Derived
        public string QualifiedName 
            => string.IsNullOrEmpty(Owner.Package) ? 
                Name : 
                string.Format("{0}.{1}", Owner.Package, Name);

        public Service(PbFile owner) {
            Owner = owner;
        }

        public override string ToString() {
            return Name;
        }
    }

    public class Rpc : Base {
        public string Name { get; set; }
        public PbType InputType { get; set; }
        public bool IsInputStream { get; set; }

        public PbType OutputType { get; set; }
        public bool IsOutputStream { get; set; }

        public override string ToString() {
            return Name;
        }
    }
}

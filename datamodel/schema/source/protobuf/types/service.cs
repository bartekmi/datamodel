using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf {
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
}

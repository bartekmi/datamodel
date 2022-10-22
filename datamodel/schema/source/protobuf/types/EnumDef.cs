using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf.data {
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
}

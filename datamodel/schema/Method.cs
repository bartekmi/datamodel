using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

using datamodel.utils;
using datamodel.toplevel;
namespace datamodel.schema {
    public class Method : Member {
        public List<DataType> ParameterTypes { get; set; } = new List<DataType>();
        public bool ShouldSerializeParameterTypes() { return ParameterTypes.Count > 0; }
        public DataType ReturnType { get; set; }

        // Derived 
        [JsonIgnore]
        public override string HumanName { get { return NameUtils.ToHuman(Name, true); } }
        [JsonIgnore]
        public string HumanFullRepresentation {
            get {
                return string.Format("{0}({1}) {2}",
                    HumanName,
                    string.Join(", ", ParameterTypes.Select(x => x.Name)),
                    ReturnType.Name);

            }
        }
    }
}
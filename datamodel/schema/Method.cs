using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using datamodel.utils;

namespace datamodel.schema {
    public class Method : IDbElement {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool ShouldSerializeDescription() { return !string.IsNullOrWhiteSpace(Description); }
        public bool Deprecated { get; set; }

        public List<DataType> ParameterTypes { get; set; } = new List<DataType>();
        public bool ShouldSerializeParameterTypes() { return ParameterTypes.Count > 0; }
        public DataType ReturnType { get; set; }
        public List<Label> Labels = new List<Label>();      // Arbitrary user-defined labels
        public bool ShouldSerializeLabels() { return Labels.Count > 0; }

        // Derived 
        [JsonIgnore]
        public string HumanName { get { return NameUtils.ToHuman(Name); } }
    }
}
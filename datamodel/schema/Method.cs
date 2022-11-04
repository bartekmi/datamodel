using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

using datamodel.utils;
using datamodel.toplevel;
namespace datamodel.schema {
    public class Method : Member {
        public List<NamedType> Inputs { get; set; } = new List<NamedType>();
        public bool ShouldSerializeParameterTypes() { return Inputs.Count > 0; }
        public List<NamedType> Outputs { get; set; } = new List<NamedType>();
        public bool ShouldSerializeOutputs() { return Outputs.Count > 0; }

        // Derived 
        [JsonIgnore]
        public override string HumanName { get { return NameUtils.ToHuman(Name, true); } }
        [JsonIgnore]
        public string HumanShortRepresentation {
            get {
                return HumanRepresentation(x => x.ToStringCompact());
            }
        }

        public string HumanRepresentation(Func<NamedType,string> convert) {
            string inputs = string.Join(", ", Inputs.Select(x => convert(x)));
            
            string outputs;
            switch (Outputs.Count) {
                case 0: outputs = ""; break;
                case 1: outputs = convert(Outputs.Single()); break;
                default: {
                    outputs = string.Join(", ", Outputs.Select(x => convert(x)));
                    outputs = string.Format("({0})", outputs);
                    break;
                }
            }
            
            return string.Format("{0}({1}) {2}", HumanName, inputs, outputs);
        }
    }

    public class NamedType {
        public string Name { get; set; }
        public DataType Type { get; set; }

        // For compact representation, it is assumed that...
        // ...for scalar parameters, Name is more significant
        // ...for Model parameters, the Name of the model is more significant
        public string ToStringCompact() {
            if (Type.ReferencedModel == null)
                return string.IsNullOrEmpty(Name) ? Type.Name : Name;
            return Type.ReferencedModel.Name;
        }

        public override string ToString() {
            return string.Format("{0} {1}", Type.Name, Name);
        }
    }
}
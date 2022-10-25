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
        public string HumanFullRepresentation {
            get {
                string inputs = string.Join(", ", Inputs.Select(x => x.ToStringCompact()));
                
                string outputs;
                switch (Outputs.Count) {
                    case 0: outputs = ""; break;
                    case 1: outputs = Outputs.Single().ToStringCompact(); break;
                    default: {
                        outputs = string.Join(", ", Outputs.Select(x => x.ToStringCompact()));
                        outputs = string.Format("({0})", outputs);
                        break;
                    }
                }
               
                return string.Format("{0}({1}) {2}", HumanName, inputs, outputs);
            }
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
                return Name;
            return Type.ReferencedModel.Name;
        }

        public override string ToString() {
            return string.Format("{0} {1}", Type.Name, Name);
        }
    }
}
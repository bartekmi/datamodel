using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace datamodel.schema.source.protobuf {
    public enum ImportType {
        None,
        Weak,
        Public,
    }
    public class Import {
        public string ImportPath { get; set; }
        public ImportType ImportType { get; set; }

        public override string ToString() {
            return ImportPath;
        }
    }
}

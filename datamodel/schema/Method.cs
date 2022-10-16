using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using datamodel.utils;
using datamodel.toplevel;

namespace datamodel.schema {
    public class Method : IDbElement {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool ShouldSerializeDescription() { return !string.IsNullOrWhiteSpace(Description); }
        public bool Deprecated { get; set; }


    }
}
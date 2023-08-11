using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using datamodel.utils;
using datamodel.toplevel;
namespace datamodel.schema {
    // Base class for Properties and Methods
    public abstract class Member : IDbElement {
        [JsonIgnore]
        public abstract string HumanName { get; }

        public string Name { get; set; }
        public string Description { get; set; }
        public bool ShouldSerializeDescription() { return !string.IsNullOrWhiteSpace(Description); }
        public bool Deprecated { get; set; }
        public List<Label> Labels { get; set; } = new List<Label>();      // Arbitrary user-defined labels
        public bool ShouldSerializeLabels() { return Labels != null && Labels.Count > 0; }

        // Hydrated
        [JsonIgnore]    // Owner causes a "Self referencing loop"
        public Model Owner { get; internal set; }

        // Derived 
        [JsonIgnore]
        public string DocUrl { get { return string.Format("{0}#{1}", UrlService.Singleton.DocUrl(Owner, false), Name); } }
    }
}
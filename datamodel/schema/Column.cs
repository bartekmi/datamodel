using System;
using Newtonsoft.Json;

using datamodel.utils;
using datamodel.toplevel;

namespace datamodel.schema {
    public class Column : IDbElement {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool CanBeEmpty { get; set; }
        public string Level1 { get; set; }
        public string Description { get; set; }
        public bool Deprecated { get; set; }
        public Enum Enum { get; set; }
        [JsonIgnore]    // Owner causes a "Self referencing loop"
        public Model Owner { get; internal set; }
        [JsonIgnore]
        public Model ReferencedModel { get; set; }

        // Derived 
        public string HumanName { get { return NameUtils.ToHuman(Name); } }
        public bool IsRef { get { return ReferencedModel != null; } }
        public string DocUrl { get { return string.Format("{0}#{1}", UrlService.Singleton.DocUrl(Owner), Name); } }
        public string[] DescriptionParagraphs {
            get {
                if (string.IsNullOrWhiteSpace(Description))
                    return new string[0];
                return Description.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            }
        }

        // Rehydrated
        public bool IsPolymorphicId { get; internal set; }
        public bool IsPolymorphicType { get; internal set; }

        public override string ToString() {
            return string.Format("{0}.{1}", Owner.Name, Name);
        }
    }
}
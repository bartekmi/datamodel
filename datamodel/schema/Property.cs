using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using datamodel.utils;
using datamodel.toplevel;

namespace datamodel.schema {
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Property : IDbElement {
        public string Name { get; set; }
        public bool CanBeEmpty { get; set; }
        public string Description { get; set; }
        public bool ShouldSerializeDescription() { return !string.IsNullOrWhiteSpace(Description); }
        public bool Deprecated { get; set; }
        private DataType _dataType = new DataType();
        
        [JsonIgnore]    // Owner causes a "Self referencing loop"
        public Model Owner { get; internal set; }
        public List<Label> Labels = new List<Label>();      // Arbitrary user-defined labels
        public bool ShouldSerializeLabels() { return Labels.Count > 0; }

        // Derived 
        [JsonIgnore]
        public string HumanName { get { return NameUtils.ToHuman(Name); } }
        [JsonIgnore]
        public bool IsRef { get { return ReferencedModel != null; } }
        [JsonIgnore]
        public string DocUrl { get { return string.Format("{0}#{1}", UrlService.Singleton.DocUrl(Owner), Name); } }

        // DataType wrappers
        public string DataType { 
            get { return _dataType.Name; }
            set { _dataType.Name = value; }
        }
        public Enum Enum { 
            get { return _dataType.Enum; }
            set { _dataType.Enum = value; }
        }
        [JsonIgnore]
        public Model ReferencedModel { 
            get { return _dataType.ReferencedModel; }
            set { _dataType.ReferencedModel = value; }
        }

        // Rehydrated
        [JsonIgnore]
        public bool IsPolymorphicId { get; internal set; }
        [JsonIgnore]
        public bool IsPolymorphicType { get; internal set; }

        public override string ToString() {
            return string.Format("{0}.{1}", Owner.Name, Name);
        }

        public void AddLabel(string name, string value) {
            Labels.Add(new Label() {
                Name = name,
                Value = value,
            });
        }

        public void AddUrl(string name, string url) {
            Labels.Add(new Label() {
                Name = name,
                Value = url,
                IsUrl = true,
            });
        }

    }
}
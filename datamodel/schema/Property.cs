using Newtonsoft.Json;

using datamodel.utils;

namespace datamodel.schema {
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Property : Member {
        public bool CanBeEmpty { get; set; }
        private DataType _dataType = new DataType();
        [JsonIgnore]
        public DataType DataTypeObj => _dataType;
        

        // Derived 
        [JsonIgnore]
        public override string HumanName { get { return NameUtils.ToHuman(Name); } }
        [JsonIgnore]
        public bool IsRef { get { return ReferencedModel != null; } }

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
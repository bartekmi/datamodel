using Newtonsoft.Json;

namespace datamodel.schema {
    // Represents a primitive, enum or reference data type. Used for
    // Properties and Method input/output types.
    public class DataType {
        public string Name { get; set; }
        public Enum Enum { get; set; }
        [JsonIgnore]
        public Model ReferencedModel { get; set; }
    }
}
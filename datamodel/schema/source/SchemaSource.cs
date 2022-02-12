using System.Collections.Generic;

namespace datamodel.schema.source {
    public abstract class SchemaSource {
        public abstract IEnumerable<Model> GetModels();
        public abstract IEnumerable<Association> GetAssociations();
    }
}

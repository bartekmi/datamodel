using System.Collections.Generic;

namespace datamodel.schema.source {
    public abstract class SchemaSource {
        public abstract string GetTitle();
        protected abstract IEnumerable<Model> GetModels();
        public abstract IEnumerable<Association> GetAssociations();

        protected virtual IEnumerable<Model> FilterModels(IEnumerable<Model> models) {
            return models;
        }

        public virtual void PostProcessSchema() {
            // Do nothing
        }

        internal IEnumerable<Model> GetFilteredModels() {
            var models = GetModels();
            return FilterModels(models);
        }
    }
}

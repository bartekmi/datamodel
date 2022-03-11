using System.Linq;
using System.Collections.Generic;

using datamodel.schema.tweaks;

namespace datamodel.schema.source {
    public abstract class SchemaSource {
        public abstract string GetTitle();
        public abstract IEnumerable<Model> GetModels();
        public abstract IEnumerable<Association> GetAssociations();

        // Tweaks to apply to the schema just after receiving the data from SchemaSource
        public IEnumerable<Tweak> Tweaks { get; set; }

        internal SchemaSource ApplyTweaks(bool postHydration) {
            TempSource source = TempSource.CloneFromSource(this);
            if (Tweaks != null)
                foreach (Tweak tweak in Tweaks.Where(x => x.PostHydration == postHydration))
                    tweak.Apply(source);

            return source;
        }
    }
}

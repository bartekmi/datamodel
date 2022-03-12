using System;
using System.Collections.Generic;

using datamodel.schema.tweaks;

namespace datamodel.schema.source {
    public abstract class SchemaSource {
        public abstract string GetTitle();
        public abstract IEnumerable<Model> GetModels();
        public abstract IEnumerable<Association> GetAssociations();

        private TempSource _tempSource;

        // Tweaks to apply to the schema just after receiving the data from SchemaSource
        public IEnumerable<Tweak> PreHydrationTweaks { get; set; }
        public IEnumerable<Tweak> PostHydrationTweaks { get; set; }

        internal SchemaSource ApplyPreHydrationTweaks() {
            if (_tempSource != null)
                throw new Exception("Should only call once");

            _tempSource = TempSource.CloneFromSource(this);
            ApplyTweaks(PreHydrationTweaks);
            return _tempSource;
        }

        internal void ApplyPostHydrationTweaks() {
            ApplyTweaks(PostHydrationTweaks);
        }

        private void ApplyTweaks(IEnumerable<Tweak> tweaks) {
            if (tweaks != null)
                foreach (Tweak tweak in tweaks)
                    tweak.Apply(_tempSource);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using datamodel.schema.tweaks;

namespace datamodel.schema.source {

    public abstract class SchemaSource {
        public abstract string GetTitle();
        public abstract IEnumerable<Model> GetModels();
        public abstract IEnumerable<Association> GetAssociations();
        public abstract IEnumerable<Parameter> GetParameters();
        public abstract void Initialize(Parameters parameters);

        private TempSource _tempSource;

        // Tweaks to apply to the schema just after receiving the data from SchemaSource
        [JsonIgnore]
        public List<Tweak> Tweaks { get; set; } = new List<Tweak>();

        internal SchemaSource ApplyPreHydrationTweaks() {
            if (_tempSource != null)
                throw new Exception("Should only call once");

            _tempSource = TempSource.CloneFromSource(this);
            ApplyTweaks(TweakApplyStep.PreHydrate);
            return _tempSource;
        }

        internal void ApplyPostHydrationTweaks() {
            ApplyTweaks(TweakApplyStep.PostHydrate);
        }

        private void ApplyTweaks(TweakApplyStep applyStep) {
            foreach (Tweak tweak in Tweaks.Where(x => x.ApplyStep == applyStep))
                tweak.Apply(_tempSource);
        }
    }
}

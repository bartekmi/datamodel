using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.source;

namespace datamodel.schema.tweaks {
    // Modify the most detailed level so as to force all derived classes to appear
    // in their own diagarm, thus offloading the diagram of the parent.
    // This is useful when a lot of very similar derived clases pollute the main diagram.
    public class MoveDerivedToPeerLevelTweak : Tweak {
        public string BaseClassName { get; set; }

        public MoveDerivedToPeerLevelTweak() : base(TweakApplyStep.PostHydrate) { }

        public override void Apply(TempSource source) {
            Model superclass = source.GetModel(BaseClassName);

            foreach (Model derived in superclass.DerivedClasses) {
                string[] levels = derived.Levels;
                int last = levels.Length - 1;
                levels[last] = superclass.Name;
                derived.Levels = levels;
            }
        }
    }
}
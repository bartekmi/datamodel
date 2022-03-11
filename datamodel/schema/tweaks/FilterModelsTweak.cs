using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.source;

namespace datamodel.schema.tweaks {
    public abstract class FilterModelsTweak : Tweak {
        public abstract IEnumerable<Model> ModelsToFilterOut(TempSource source);

        public override void Apply(TempSource source) {
            IEnumerable<Model> toRemoveList = ModelsToFilterOut(source);
            HashSet<string> toRemove = new HashSet<string>(toRemoveList.Select(x => x.QualifiedName));

            // Remove Models
            var newModels = source.GetModels().Where(x => !toRemove.Contains(x.QualifiedName)).ToList();
            source.SetModels(newModels);

            // Remove Associations
            foreach (Association association in source.Associations.ToList()) {
                if (toRemove.Contains(association.OwnerSide) ||
                    toRemove.Contains(association.OtherSide))
                    source.Associations.Remove(association);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.source;

namespace datamodel.schema.tweaks {
    public class AddInheritanceTweak : Tweak {
        public string ParentQualifiedName;
        public string DerviedQualifiedName;

        public override void Apply(TempSource source) {
            Model parent = source.GetModel(ParentQualifiedName);
            Model derived = source.GetModel(DerviedQualifiedName);

            derived.SuperClassName = ParentQualifiedName;

            // Remove every Prop/Column from derived that exists in parent
            foreach (Column propInDerived in derived.AllColumns.ToList()) {
                Column propInParent = parent.FindColumn(propInDerived.Name, propInDerived.DataType);
                if (propInParent != null)
                    derived.AllColumns.Remove(propInDerived);
            }

            // Remove every duplicate owned association from Derived
            foreach (Association derivedAssoc in source.Associations.ToList()) {
                if (derivedAssoc.OwnerSide == derived.QualifiedName) {
                    Association parentAssoc = source.FindOwnedAssociation(parent.QualifiedName, derivedAssoc);
                    if (parentAssoc != null)
                        source.Associations.Remove(derivedAssoc);
                }
            }
        }
    }
}
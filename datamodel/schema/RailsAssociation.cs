using System;
using System.Linq;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using datamodel.utils;

namespace datamodel.schema {

    internal enum AssociationKind {
        BelongsTo,
        HasOne,
        HasMany,
        HasAndBelongsToMany,
        Through,
    }

    public class Options {
        internal bool Polymorphic;
        internal string ClassName;
        internal string As;
        internal bool Destroy;
        internal string InverseOf;
        internal string Through;
        internal string ForeignKey;
        internal string Source;
    }

    public class RailsAssociation {

        internal AssociationKind Kind;
        internal string Name;
        internal string OwningModel;    // The model to which this association 'belongs'
        internal string OtherModel;     // The model at the other end of the association
        internal string ForeignKey;
        internal string InverseOf;
        internal string PluralName;
        internal Options Options;

        // Unused and possible unnecessary
        internal string ForeignType;
        internal string ClassName;
        internal string Type;

        // Derived
        internal string UnqualifiedClassName {
            get {
                int index = OtherModel.IndexOf("::");
                if (index >= 0)
                    return OtherModel.Substring(index + 2);
                return OtherModel;
            }
        }
        internal bool IsReverse {
            get {
                switch (Kind) {
                    case AssociationKind.HasOne:
                    case AssociationKind.HasMany:
                        return true;
                    case AssociationKind.HasAndBelongsToMany:
                        // See comment on Schema.MakeKey()
                        if (OwningModel == OtherModel) {
                            if (ForeignKey == InverseOf)
                                throw new NotImplementedException();
                            return ForeignKey.CompareTo(InverseOf) > 0;
                        }
                        return OwningModel.CompareTo(OtherModel) > 0;
                    default:
                        return false;
                }
            }
        }
        internal bool IsHABTM {
            get {
                return OwningModel.Contains("HABTM");
            }
        }
        internal bool IsPolymorphicBelongsTo {
            get {
                return Kind == AssociationKind.BelongsTo && Options.Polymorphic;
            }
        }

        // Set in post-processing
        internal Column FkColumn;
    }
}

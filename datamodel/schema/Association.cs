using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using datamodel.utils;

namespace datamodel.schema {

    // This has direct bearing how an end of an association is shown on the graph
    public enum Multiplicity {
        ZeroOrOne,      // This side of the relationship can be associated with one optional instance
        One,            // This side of the relationship is associated with exactly one instance
        Many,           // This side of the relationship is associated with many instances
        Aggregation,    // Expresses parent of an "ownership" relationship like 1 car has 4 wheels.
    }

    public class Association {
        public string FkSide { get; set; }
        public string OtherSide { get; set; }
        public bool SourceOptional { get; set; }
        public bool DestinationOptional { get; set; }
        public List<RailsAssociation> RailsAssociations = new List<RailsAssociation>();

        // Hydrated
        public Model OtherSideModel { get; set; }
        public Model FkSideModel { get; set; }

        // Derived
        public bool Recursive { get { return OtherSide == FkSide; } }
        public Column FkColumn { get { return RailsAssociations.Select(x => x.FkColumn).FirstOrDefault(x => x != null); } }
        public string DocUrl { get { return FkColumn == null ? null : FkColumn.DocUrl; } }
        public string Description {
            get {
                if (FkColumn == null)
                    return "Unknown";
                return FkColumn.Description == null ? FkColumn.HumanName : FkColumn.Description;
            }
        }
        public bool IsPolymorphic { get { return OtherSidePolymorphicName != null; } }
        public string OtherSidePolymorphicName { get { return RailsAssociations.First().Options.As; } }

        public string RoleOppositeFK {
            get {
                RailsAssociation belongsTo = RailsAssociations.FirstOrDefault(x => x.Kind == AssociationKind.BelongsTo);
                if (belongsTo == null)
                    return null;

                if (FkInfo.StripId(belongsTo.ForeignKey).Replace("_", "").ToLower() ==
                    belongsTo.UnqualifiedClassName.ToLower())
                    return null;        // The FK name is no different from the entity it points to. Boring.

                return FkInfo.FkColumnToHuman(belongsTo.ForeignKey);
            }
        }
        public string RoleByFK {
            get {
                // For now, assuming that the FK side of a polymorphic association is boring
                if (IsPolymorphic)
                    return null;

                RailsAssociation hasOne = RailsAssociations.FirstOrDefault(x => x.Kind == AssociationKind.HasOne);
                if (hasOne != null) {
                    if (hasOne.Name.Replace("_", "").ToLower() ==
                        hasOne.UnqualifiedClassName.ToLower())
                        return null;        // Boring
                    else
                        return NameUtils.SnakeCaseToHuman(hasOne.Name);
                }

                RailsAssociation hasMany = RailsAssociations.FirstOrDefault(x => x.Kind == AssociationKind.HasMany);
                if (hasMany != null) {
                    if (hasMany.Name.ToLower() ==
                        hasMany.PluralName.ToLower())
                        return null;        // Boring
                    else
                        return NameUtils.SnakeCaseToHuman(hasMany.Name);
                }

                return null;
            }
        }

        public Multiplicity FkSideMultiplicity { get; set; }
        public Multiplicity OtherSideMultiplicity { get; set; }

        public Association(RailsAssociation railsAssociation, RailsAssociation reverseRailsAssociation) {
            RailsAssociations.Add(railsAssociation);
            if (reverseRailsAssociation != null)
                RailsAssociations.Add(reverseRailsAssociation);
        }

        override public string ToString() {
            return string.Format("Association from {0} to {1} {2}", FkSide, OtherSide, IsPolymorphic ? "(Polymorphic)" : "");
        }
    }
}

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
        public bool Indirect { get; set; }
        public bool SourceOptional { get; set; }
        public bool DestinationOptional { get; set; }
        public List<RailsAssociation> RailsAssociations = new List<RailsAssociation>();

        // Hydrated
        public Table OtherSideTable { get; set; }
        public Table FkSideTable { get; set; }

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

        // // The "Source" end of the relationship (by the Rails definition) is the end to which the FK points
        // public Multiplicity SourceMultiplicity {
        //     get {
        //         bool isSingle = Cardinality == Cardinality.one_to_one || Cardinality == Cardinality.one_to_many;
        //         return ToMultiplicity(isSingle, SourceOptional);
        //     }
        // }

        // // The "Destination" end of the relationship (by the Rails definition) is the end which has the FK
        // public Multiplicity DestinationMultiplicity {
        //     get {
        //         bool isSingle = Cardinality == Cardinality.one_to_one || Cardinality == Cardinality.many_to_one;
        //         return ToMultiplicity(isSingle, DestinationOptional);
        //     }
        // }

        // private Multiplicity ToMultiplicity(bool single, bool optional) {
        //     if (!single)
        //         return Multiplicity.Many;

        //     return optional ? Multiplicity.ZeroOrOne : Multiplicity.One;
        // }

        // public override string ToString() {
        //     return string.Format("{0}-{1} ({2})", Source, Destination, Cardinality);
        // }
    }
}

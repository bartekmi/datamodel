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
        public string RoleOppositeFK {get; set; }
        public string RoleByFK {get; set; }
        public Multiplicity FkSideMultiplicity { get; set; }
        public Multiplicity OtherSideMultiplicity { get; set; }
        public string Description { get; set; }

        // Hydrated
        public Model OtherSideModel { get; set; }
        public Model FkSideModel { get; set; }
        public Column FkColumn { get; set; } 

        // Derived
        public bool SourceOptional { get { return FkSideMultiplicity == Multiplicity.ZeroOrOne; } }
        public bool DestinationOptional { get { return OtherSideMultiplicity == Multiplicity.ZeroOrOne; } }
        public bool Recursive { get { return OtherSide == FkSide; } }
        public string DocUrl { get { return FkColumn == null ? null : FkColumn.DocUrl; } }
        public bool IsPolymorphic { get { return PolymorphicName != null; } }
        public string PolymorphicName {
            get {
                // At this time, there is no use-case for Polymorphic Associations
                return null;
            }
        }
        public string PolymorphicReverseName {
            get {
                // At this time, there is no use-case for Polymorphic Associations
                return null;
            }
        }

        override public string ToString() {
            return string.Format("FK: {0} to {1} {2}", FkSide, OtherSide, IsPolymorphic ? "(Polymorphic)" : "");
        }
    }
}

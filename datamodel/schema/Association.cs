using System;
using Newtonsoft.Json;

namespace datamodel.schema {

    // This has direct bearing how an end of an association is shown on the graph
    public enum Multiplicity {
        ZeroOrOne,      // This side of the relationship can be associated with one optional instance
        One,            // This side of the relationship is associated with exactly one instance
        Many,           // This side of the relationship is associated with many instances
        Aggregation,    // Expresses parent of an "ownership" relationship like 1 car has 4 wheels.
    }

    public class Association {
        public string OwnerSide { get; set; }
        public string OwnerRole {get; set; }
        public Multiplicity OwnerMultiplicity { get; set; }

        public string OtherSide { get; set; }
        public string OtherRole {get; set; }
        public Multiplicity OtherMultiplicity { get; set; }

        public string Description { get; set; }

        // Hydrated
        [JsonIgnore]
        public Model OtherSideModel { get; set; }
        [JsonIgnore]
        public Model FkSideModel { get; set; }
        [JsonIgnore]
        public Column FkColumn { get; set; } 

        // Derived
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
            return string.Format("{0} to {1} {2}", OwnerSide, OtherSide, IsPolymorphic ? "(Polymorphic)" : "");
        }
    }
}

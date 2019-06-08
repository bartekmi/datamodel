using System;
using System.Collections.Generic;
using System.Text;

namespace datamodel.schema {

    // Snake case to allow for direct import from Ruby output file without ranslation
    public enum Cardinality {
        one_to_one,
        one_to_many,
        many_to_one,
        many_to_many,
    }

    // This has direct bearing how an end of an association is shown on the graph
    public enum Multiplicity {
        ZeroOrOne,      // This side of the relationship can be associated with one optional instance
        One,            // This side of the relationship is associated with exactly one instance
        Many,           // This side of the relationship is associated with many instances
        Aggregation,    // Expresses parent of an "ownership" relationship like 1 car has 4 wheels.
    }

    public class Association {
        public string Source { get; set; }
        public string Destination { get; set; }
        public bool Indirect { get; set; }
        public bool Mutual { get; set; }
        public bool Recursive { get; set; }
        public Cardinality Cardinality { get; set; }
        public bool SourceOptional { get; set; }
        public bool DestinationOptional { get; set; }

        // Hydrated
        public Table SourceTable { get; set; }
        public Table DestinationTable { get; set; }

        // Derived
        public Multiplicity SourceMultiplicity {
            get {
                bool isSingle = Cardinality == Cardinality.one_to_one || Cardinality == Cardinality.one_to_many;
                return ToMultiplicity(isSingle, SourceOptional);
            }
        }
        public Multiplicity DestinationMultiplicity {
            get {
                bool isSingle = Cardinality == Cardinality.one_to_one || Cardinality == Cardinality.many_to_one;
                return ToMultiplicity(isSingle, DestinationOptional);
            }
        }

        private Multiplicity ToMultiplicity(bool single, bool optional) {
            if (!single)
                return Multiplicity.Many;

            return optional ? Multiplicity.ZeroOrOne : Multiplicity.One;
        }

        public override string ToString() {
            return string.Format("{0}-{1} ({2})", Source, Destination, Cardinality);
        }
    }
}

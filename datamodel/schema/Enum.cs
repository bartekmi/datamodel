using System;
using System.Collections.Generic;
using System.Linq;

namespace datamodel.schema {
    public class Enum {

        public string Name { get; set; }
        public string Description { get; set; }
        public bool ShouldSerializeDescription() { return !string.IsNullOrWhiteSpace(Description); }

        // Key is the enum value; value is the description
        private Dictionary<string, string> _values = new Dictionary<string, string>();

        public IEnumerable<KeyValuePair<string, string>> Values {
            get {
                // We specifically don't want to sort this in any way because the original
                // ordering of the enum values may be significant.
                // If this becomes an issue in the future, this could be made a per-SchemaSource
                // property. 
                // In particular, for the protobuf schema source, original ordering must
                // be preserved.
                return _values;
            }
        }

        public void SetDescription(string value, string description) {
            _values[value] = description;
        }

        public string GetDescription(string value) {
            return _values[value];
        }

        public void Add(string value, string description) {
            _values[value] = description;
        }
    }
}
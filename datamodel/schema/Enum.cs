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
                return _values.OrderBy(x => x.Key);
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
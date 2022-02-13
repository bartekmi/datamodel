using System;
using System.Collections.Generic;
using System.Linq;

namespace datamodel.schema {
    public class Enum {

        private List<KeyValuePair<string, string>> _values = new List<KeyValuePair<string, string>>();

        public IEnumerable<KeyValuePair<string, string>> Values {
            get {
                return _values.OrderBy(x => x.Key);
            }
        }

        internal void Add(string value, string description) {
            _values.Add(new KeyValuePair<string, string>(value, description));
        }
    }
}
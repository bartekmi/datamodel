using System;
using System.Collections.Generic;

namespace datamodel.schema {
    public class Enum {

        public List<KeyValuePair<int, string>> Values = new List<KeyValuePair<int, string>>();

        internal void Add(int number, string string_value) {
            Values.Add(new KeyValuePair<int, string>(number, string_value));
        }
    }
}
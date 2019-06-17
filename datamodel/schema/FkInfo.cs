using System;
using datamodel.utils;

namespace datamodel.schema {

    public class FkInfo {

        public Table ReferencedTable { get; set; }
        public string ThisEndRole { get; set; }

        public string OtherEndRole { get; set; }
        public bool OtherEndIsAggregation { get; set; }        // Means that entity on this end of the relationship can only live in the context of the other
    }
}
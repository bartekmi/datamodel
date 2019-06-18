using System;
using datamodel.utils;

namespace datamodel.schema {

    public class FkInfo {

        public Table ReferencedTable { get; set; }
        public bool OtherEndIsAggregation { get; set; }        // Means that entity on this end of the relationship can only live in the context of the other

        internal static string FkColumnToHuman(string foreignKey) {
            if (string.IsNullOrWhiteSpace(foreignKey))
                return null;

            return NameUtils.SnakeCaseToHuman(StripId(foreignKey));
        }

        internal static string StripId(string foreignKey) {
            if (foreignKey != null && foreignKey.EndsWith("_id"))
                return foreignKey.Substring(0, foreignKey.Length - 3);
            return foreignKey;
        }
    }
}
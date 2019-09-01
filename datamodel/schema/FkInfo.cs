using System;
using datamodel.utils;

namespace datamodel.schema {

    public class FkInfo {

        public Model ReferencedModel { get; set; }

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
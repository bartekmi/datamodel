using System;
using datamodel.utils;
using Newtonsoft.Json;

namespace datamodel.schema {

    public class FkInfo {

        [JsonIgnore]
        public Model ReferencedModel { get; set; }

        // Derived (Do not remove - helpful for debugging)
        public string ModelName {
            get { return ReferencedModel == null ? "<unset>" : ReferencedModel.Name; } 
        }

        internal static string FkColumnToHuman(string foreignKey) {
            if (string.IsNullOrWhiteSpace(foreignKey))
                return null;

            return NameUtils.ToHuman(StripId(foreignKey));
        }

        internal static string StripId(string foreignKey) {
            if (foreignKey != null && foreignKey.EndsWith("_id"))
                return foreignKey.Substring(0, foreignKey.Length - 3);
            return foreignKey;
        }
    }
}
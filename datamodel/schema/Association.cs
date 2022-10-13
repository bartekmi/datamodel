using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using datamodel.utils;

namespace datamodel.schema {

    // This has direct bearing how an end of an association is shown on the graph
    public enum Multiplicity {
        ZeroOrOne,      // This side of the relationship can be associated with one optional instance
        One,            // This side of the relationship is associated with exactly one instance
        Many,           // This side of the relationship is associated with many instances
        Aggregation,    // Expresses parent of an "ownership" relationship like 1 car has 4 wheels.
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Association {
        public string OwnerSide { get; set; }
        public string OwnerRole { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]        
        [JsonConverter(typeof(StringEnumConverter))]        
        public Multiplicity OwnerMultiplicity { get; set; }

        public string OtherSide { get; set; }
        public string OtherRole { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]        
        [JsonConverter(typeof(StringEnumConverter))]        
        public Multiplicity OtherMultiplicity { get; set; }

        public string Description { get; set; }
        public bool ShouldSerializeDescription() { return !string.IsNullOrWhiteSpace(Description); }

        // Hydrated
        [JsonIgnore]
        public Model OtherSideModel { get; set; }
        [JsonIgnore]
        public Model OwnerSideModel { get; set; }
        [JsonIgnore]
        public Column RefColumn { get; set; }

        // Derived
        [JsonIgnore]
        public string InterestingOwnerRole {
            get {
                if (IsBoringRoleName(OwnerRole, OwnerSideModel))
                    return null;
                return OwnerRole;
            }
        }
        [JsonIgnore]
        public string InterestingOtherRole {
            get {
                if (IsBoringRoleName(OtherRole, OtherSideModel))
                    return null;
                return OtherRole;
            }
        }
        [JsonIgnore]
        public string DocUrl { get { return RefColumn == null ? null : RefColumn.DocUrl; } }
        [JsonIgnore]
        public bool IsPolymorphic { get { return PolymorphicName != null; } }
        [JsonIgnore]
        public string PolymorphicName {
            get {
                // At this time, there is no use-case for Polymorphic Associations
                return null;
            }
        }
        [JsonIgnore]
        public string PolymorphicReverseName {
            get {
                // At this time, there is no use-case for Polymorphic Associations
                return null;
            }
        }


        // A role name is considered "boring" if it contributes no meaningful information
        // over and above the type that it lies next to. For example, if the type is
        // "Application User", then the role "users" can be discarded
        private bool IsBoringRoleName(string role, Model model) {
            if (role == null)
                return true;    // Nothing more boring than no information!

            HashSet<string> modelWords = new HashSet<string>(NameUtils.ToWords(model.Name));

            foreach (string roleWord in NameUtils.ToWords(role)) {
                if (modelWords.Contains(roleWord) ||
                    modelWords.Contains(Depluralize(roleWord))) {
                    // Keep going... boring so far
                } else
                    return false;   // We found a new role word - not boring! 
            }

            return true;    // All role words found in model words - boring.
        }

        private string Depluralize(string word) {
            if (word.EndsWith("ses"))
                return word[0..^2];
            if (word.EndsWith("s"))
                return word[0..^1];
            return word;
        }

        override public string ToString() {
            return string.Format("{0} to {1} {2}", OwnerSide, OtherSide, IsPolymorphic ? "(Polymorphic)" : "");
        }
    }
}

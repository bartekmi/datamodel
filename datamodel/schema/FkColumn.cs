using System;
using datamodel.utils;

namespace datamodel.schema {

    public enum Multiplicity {
        One,
        Many
    }

    public class FkColumn : Column {

        public const string ID_SUFFIX = "_id";

        public Table ReferencedTable { get; set; }
        public string ThisEndRole { get; set; }
        public Multiplicity ThisEndMultiplicity { get; set; }

        public string OtherEndRole { get; set; }
        public bool OtherEndIsAggregation { get; set; }        // Means that entity on this end of the relationship can only live in the context of the other

        // Derived
        public string OtherTableName {
            get {
                if (!DbName.EndsWith(ID_SUFFIX))
                    throw new Exception("Bad fk column name " + DbName);

                string singular = DbName.Substring(0, DbName.Length - ID_SUFFIX.Length);
                string plural = NameUtils.Pluralize(singular);

                return plural;
            }
        }

        public FkColumn(Table owner) : base(owner) {
            ThisEndMultiplicity = Multiplicity.Many;      // The default
        }
    }
}
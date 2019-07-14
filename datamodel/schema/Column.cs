using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using datamodel.utils;

namespace datamodel.schema {
    public enum DataType {
        String,
        Text,     // Long string
        Integer,
        BigInt,
        Decimal,
        Float,
        Boolean,
        DateTime,
        Date,
        NumRange,
        Json,
        Jsonb,
        Uuid,
        Citext,
        Hstore,
        Geometry,
        Inet,
        Other
    }

    public class Column : IDbElement {
        public string DbName { get; set; }
        public string DbTypeString { get; set; }
        public DataType DbType { get; set; }
        public string Team { get; set; }
        public string Description { get; set; }
        public Visibility Visibility { get; set; }
        public bool Deprecated { get; set; }
        public string Issue { get; set; }
        public bool IsMandatory { get; set; }
        public string Enum { get; set; }
        public string Group { get; set; }
        public Table Owner { get; private set; }

        // Derived 
        public string HumanName { get { return NameUtils.SnakeCaseToHuman(DbName); } }
        public bool IsFk { get { return FkInfo != null; } }
        public string DocUrl { get { return string.Format("{0}#{1}", Owner.DocUrl, DbName); } }
        public string[] DescriptionParagraphs {
            get {
                if (string.IsNullOrWhiteSpace(Description))
                    return new string[0];
                return Description.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            }
        }

        // Relationships
        public FkInfo FkInfo { get; set; }

        public Column(Table owner) {
            Owner = owner;
        }
    }
}
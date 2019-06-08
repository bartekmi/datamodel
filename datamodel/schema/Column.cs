using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using datamodel.utils;

namespace datamodel.schema {
    public enum DataType {
        String,
        Text,     // Long string, I believe
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
        Inet
    }

    public class Column : IDbElement {
        public string DbName { get; set; }
        public DataType DbType { get; set; }
        public string DbTypeString { get; set; }
        public string Team { get; set; }
        public string Description { get; set; }
        public Visibility Visibility { get; set; }
        public bool IsObsolete { get; set; }
        public string Issue { get; set; }
        public bool IsMandatory { get; set; }
        public string Enum { get; set; }
        public string Group { get; set; }
        public Table Owner { get; private set; }

        // Derived 
        public string HumanName { get { return NameUtils.SnakeCaseToHuman(DbName); } }
        public bool IsFk { get { return this is FkColumn; } }

        public Column(Table owner) {
            Owner = owner;
        }
    }
}
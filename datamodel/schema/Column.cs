using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using datamodel.utils;
using datamodel.toplevel;

namespace datamodel.schema {
    public enum DataType {
        String,
        Text,     // Long string
        Integer,
        Enum,
        BigInt,
        Decimal,
        Float,
        Boolean,
        DateTime,
        Date,
        Json,
        Uuid,
        Bytes,
        Other
    }

    public class Column : IDbElement {
        public string Name { get; set; }
        public string DbTypeString { get; set; }
        public DataType DbType { get; set; }
        public bool IsNull { get; set; }
        public string[] Validations { get; set; }
        public string Team { get; set; }
        public string Description { get; set; }
        public bool Deprecated { get; set; }
        public Enum Enum { get; set; }
        public Model Owner { get; private set; }

        // Derived 
        public string HumanName { get { return NameUtils.SnakeCaseToHuman(Name); } }
        public bool IsFk { get { return FkInfo != null; } }
        public string DocUrl { get { return string.Format("{0}#{1}", UrlService.Singleton.DocUrl(Owner), Name); } }
        public string[] DescriptionParagraphs {
            get {
                if (string.IsNullOrWhiteSpace(Description))
                    return new string[0];
                return Description.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            }
        }
        public bool IsMandatory {
            get {
                return !IsNull || Validations.Contains("presence");
            }
        }

        // Rehydrated
        public bool IsPolymorphicId { get; internal set; }
        public bool IsPolymorphicType { get; internal set; }

        // Relationships
        public FkInfo FkInfo { get; set; }

        public Column(Model owner) {
            Owner = owner;
        }

        public override string ToString() {
            return string.Format("{0}.{1}", Owner.Name, Name);
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using datamodel.utils;

namespace datamodel.schema {
    public enum Visibility {
        Domain,
        Implementation
    }

    public enum TableType {
        Regular,
        LookUp,
        Join
    }

    public class Table : IDbElement {
        public string ClassName { get; set; }
        public string SuperClassName { get; set; }
        public string DbName { get; set; }
        public List<Column> AllColumns = new List<Column>();
        public TableType Type { get; set; }
        public string Team { get; set; }
        public string Description { get; set; }
        public Visibility Visibility { get; set; }
        public bool IsObsolete { get; set; }
        public string Issue { get; set; }
        public string Group { get; set; }
        public string ModelPath { get; set; }

        // Hydrated
        public Table Superclass { get; set; }

        // Derived
        public string HumanName { get { return ClassName; } }      // May tweak in future - 
        public IEnumerable<Column> RegularColumns { get { return AllColumns.Where(x => x.GetType() == typeof(Column)); } }
        public IEnumerable<FkColumn> FkColumns { get { return AllColumns.OfType<FkColumn>(); } }

        public string AnnotationFilePath {
            get {
                string noExtension = Path.GetFileNameWithoutExtension(ModelPath);
                string path = Path.Combine(Path.GetDirectoryName(ModelPath), noExtension + ".yaml");
                return path;
            }
        }

        public Column FindColumn(string dbColumnName) {
            return AllColumns.SingleOrDefault(x => x.DbName.ToLower() == dbColumnName.ToLower());
        }

        public override string ToString() {
            return DbName;
        }
    }
}
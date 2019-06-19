using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using datamodel.utils;
using datamodel.datadict.html;

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
        public bool IsSpecialized { get; set; }
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
        public string HumanName { get { return NameUtils.MixedCaseToHuman(ClassName); } }
        public IEnumerable<Column> RegularColumns { get { return AllColumns.Where(x => !x.IsFk); } }
        public IEnumerable<Column> FkColumns { get { return AllColumns.Where(x => x.IsFk); } }

        public string DocUrl { get { return UrlUtils.ToAbsolute(string.Format("{0}/{1}.html", Team, ClassName)); } }
        public string SvgUrl { get { return UrlUtils.ToAbsolute(string.Format("{0}.svg", Team)); } }

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
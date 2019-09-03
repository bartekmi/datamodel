using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using datamodel.utils;

namespace datamodel.schema {
    public enum Visibility {
        Domain,
        Implementation
    }

    public enum ModelType {
        Regular,
        LookUp,
        Join
    }

    public class Model : IDbElement {
        #region Ruby Reflection Properties
        // The fully-qualified Ruby Class-Name of the Model
        public string ClassName { get; set; }

        // The fully-qualified Ruby Class-Name of the super-class of the Model
        public string SuperClassName { get; set; }

        // Is this model abstract, as indicated by 'self.abstract_class = true'
        public bool IsAbstract { get; set; }

        // Name of the corresponding Database table
        public string DbName { get; set; }

        public List<Column> AllColumns = new List<Column>();
        #endregion


        #region Other Properties (Yaml Annotation, parsed from file)
        // The classification of this Model. Currently not used, but the intention is to 
        // Render the graph differently depending on this type. For example, a Join table
        // may be rendered asa man-to-many associations in more terse verison of the graph.
        public ModelType Type { get; set; }

        // Team to which this Model belongs, as extracted from the header of the Ruby file.
        public string Team { get; set; }

        // Engine to which this Model belongs, as extracted from the directory hierarchy
        public string Engine { get; set; }

        // Description of the Model as extracted from Yaml annotation files
        public string Description { get; set; }

        // Not currently used, but in the future, more terse graph representations 
        // would only show Domain-level models as opposed to internal implementation models
        public Visibility Visibility { get; set; }

        // Is this model Deprecated - as per Yaml annotation file
        public bool Deprecated { get; set; }

        public string Issue { get; set; }

        public string Group { get; set; }

        // The file-system path to the Model Ruby file
        public string ModelPath { get; set; }
        #endregion


        #region Hydrated
        public Model Superclass { get; set; }
        #endregion


        #region Derived
        public string HumanName { get { return NameUtils.MixedCaseToHuman(UnqualifiedClassName); } }
        public string UnqualifiedClassName { get { return ExtractUnqualifiedClassName(ClassName); } }
        public string Module { get { return ExtractModule(ClassName); } }
        public IEnumerable<Column> RegularColumns { get { return AllColumns.Where(x => !x.IsFk); } }
        public IEnumerable<Column> FkColumns { get { return AllColumns.Where(x => x.IsFk); } }
        public string SanitizedClassName { get { return FileUtils.SanitizeFilename(ClassName); } }

        public string AnnotationFilePath {
            get {
                string noExtension = Path.GetFileNameWithoutExtension(ModelPath);
                string path = Path.Combine(Path.GetDirectoryName(ModelPath), noExtension + ".yaml");
                return path;
            }
        }

        public static string ExtractUnqualifiedClassName(string qualifiedClassName) {
            return qualifiedClassName.Split("::").Last();
        }

        public static string ExtractModule(string qualifiedClassName) {
            string[] pieces = qualifiedClassName.Split("::");
            if (pieces.Length > 1)
                return string.Join("::", pieces.Take(pieces.Length - 1));
            return null;
        }

        #endregion

        public Column FindColumn(string dbColumnName) {
            return AllColumns.SingleOrDefault(x => x.DbName.ToLower() == dbColumnName.ToLower());
        }

        public override string ToString() {
            return ClassName;
        }
    }
}
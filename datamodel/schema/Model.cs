using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using datamodel.utils;
using datamodel.metadata;

namespace datamodel.schema {
    public class Model : IDbElement {
        // The fully-qualified Ruby Class-Name of the super-class of the Model
        public string SuperClassName { get; set; }

        // Is this model abstract, as indicated by 'self.abstract_class = true'
        public bool IsAbstract { get; set; }

        // Name of the corresponding Database table
        public string Name { get; set; }

        // Team to which this Model belongs, as extracted from the header of the Ruby file.
        public string Team { get; set; }

        // Engine to which this Model belongs, as extracted from the directory hierarchy
        public string Engine { get; set; }

        // Set if module, normally extracted from ClassName, was over-ridden in a
        // visualizations.yaml file
        public string ModuleOverride { get; set; }

        // Description of the Model as extracted from Yaml annotation files
        public string Description { get; set; }

        // Is this model Deprecated - as per Yaml annotation file
        public bool Deprecated { get; set; }

        // Associations
        public List<Column> AllColumns { get; internal set; }


        #region Re-Hydrated
        public Model Superclass { get; set; }
        #endregion


        #region Derived
        public string HumanName { get { return NameUtils.MixedCaseToHuman(UnqualifiedClassName); } }
        public string UnqualifiedClassName { get { return ExtractUnqualifiedClassName(Name); } }
        public string Module { get { return ExtractModule(Name); } }
        public IEnumerable<Column> RegularColumns { get { return AllColumns.Where(x => !x.IsFk); } }
        public IEnumerable<Column> FkColumns { get { return AllColumns.Where(x => x.IsFk); } }
        public string SanitizedClassName { get { return FileUtils.SanitizeFilename(Name); } }
        public bool HasPolymorphicInterfaces { get { return PolymorphicInterfaces.Any(); } }
        public string ColorString { get { return TeamInfo.GetHtmlColorForTeam(Team); } }

        public List<Association> FkAssociations {
            get { return Schema.Singleton.FkAssociationsForModel(this); }
        }

        public IEnumerable<PolymorphicInterface> PolymorphicInterfaces {
            get { return Schema.Singleton.InterfacesForModel(this); }
        }

        public IEnumerable<Association> PolymorphicAssociations {
            get {
                return PolymorphicInterfaces
                    .SelectMany(x => Schema.Singleton.PolymorphicAssociationsForInterface(x));
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
            return AllColumns.SingleOrDefault(x => x.Name.ToLower() == dbColumnName.ToLower());
        }

        public override string ToString() {
            return Name;
        }
    }
}
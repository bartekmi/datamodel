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

        // Three level of hierarcy... Eventually, we'd like to make depth arbitrary
        public string Level1 { get; set; }
        public string Level2 { get; set; }
        public string Level3 { get; set; }

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
        public IEnumerable<Column> RegularColumns { get { return AllColumns.Where(x => !x.IsFk); } }
        public IEnumerable<Column> FkColumns { get { return AllColumns.Where(x => x.IsFk); } }
        public string SanitizedClassName { get { return FileUtils.SanitizeFilename(Name); } }
        public bool HasPolymorphicInterfaces { get { return PolymorphicInterfaces.Any(); } }
        public string ColorString { get { return Level1Info.GetHtmlColorForLevel1(Level1); } }

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

        #endregion

        public Column FindColumn(string dbColumnName) {
            return AllColumns.SingleOrDefault(x => x.Name.ToLower() == dbColumnName.ToLower());
        }

        public override string ToString() {
            return Name;
        }
    }
}
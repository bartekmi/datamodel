using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

using datamodel.utils;
using datamodel.metadata;

namespace datamodel.schema {
    public class Label {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Model : IDbElement {
        // The fully-qualified Ruby Class-Name of the super-class of the Model
        public string SuperClassName { get; set; }

        // Is this model abstract, as indicated by 'self.abstract_class = true'
        public bool IsAbstract { get; set; }

        // If set, the only purpose of this Model is to serve as a "list" of this type
        // - if referenced from another model, the association will be converted to a many-association
        // - The "List" model itself will be dropped
        public string ListSemanticsForType { get; set; }

        // Name of the Model
        public string Name { get; set; }

        // This (possibly longer) name must be guaranteed to be globally unique
        public string QualifiedName { get; set; }

        // Some schemas - e.g. Swagger - have a version attached to each Model
        public string Version { get; set; }
        public string QualifiedNameLessVersion { get; set; }

        // Three level of hierarcy... Eventually, we'd like to make depth arbitrary
        public string Level1 { get; set; }
        public string Level2 { get; set; }
        public string Level3 { get; set; }

        // Description of the Model as extracted from Yaml annotation files
        public string Description { get; set; }

        // Is this model Deprecated - as per Yaml annotation file
        public bool Deprecated { get; set; }

        // Arbitrary user-defined labels
        public List<Label> Labels = new List<Label>();

        // Associations
        public List<Column> AllColumns { get; internal set; }


        #region Re-Hydrated
        public Model Superclass { get; set; }
        #endregion


        #region Derived
        public string HumanName { get { return NameUtils.MixedCaseToHuman(Name); } }
        [JsonIgnore]
        public IEnumerable<Column> RegularColumns { get { return AllColumns.Where(x => !x.IsFk); } }
        [JsonIgnore]
        public IEnumerable<Column> FkColumns { get { return AllColumns.Where(x => x.IsFk); } }
        public string SanitizedQualifiedName { get { return FileUtils.SanitizeFilename(QualifiedName); } }
        public bool HasPolymorphicInterfaces { get { return PolymorphicInterfaces.Any(); } }
        public string ColorString { get { return Level1Info.GetHtmlColorForLevel1(Level1); } }

        [JsonIgnore]
        public List<Association> FkAssociations {
            get { return Schema.Singleton.FkAssociationsForModel(this); }
        }

        [JsonIgnore]
        public IEnumerable<PolymorphicInterface> PolymorphicInterfaces {
            get { return Schema.Singleton.InterfacesForModel(this); }
        }

        [JsonIgnore]
        public IEnumerable<Association> PolymorphicAssociations {
            get {
                return PolymorphicInterfaces
                    .SelectMany(x => Schema.Singleton.PolymorphicAssociationsForInterface(x));
            }
        }

        #endregion

        public void AddLabel(string name, string value) {
            Labels.Add(new Label() {
                Name = name,
                Value = value,
            });
        }

        public Column FindColumn(string dbColumnName) {
            return AllColumns.SingleOrDefault(x => x.Name.ToLower() == dbColumnName.ToLower());
        }

        public IEnumerable<Model> SelfAndDescendents() {
            HashSet<Model> models = new HashSet<Model>();
            SelfAndAllDescendentsRecursive(models, this);
            return models;
        }

        private void SelfAndAllDescendentsRecursive(HashSet<Model> models, Model model) {
            if (models.Contains(model))
                return;

            models.Add(model);

            foreach (Column column in model.FkColumns)
                SelfAndAllDescendentsRecursive(models, column.FkInfo.ReferencedModel);
        }


        public override string ToString() {
            return Name;
        }
    }
}
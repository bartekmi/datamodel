using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

using datamodel.utils;
using datamodel.metadata;
using datamodel.toplevel;

namespace datamodel.schema {
    public class Label {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsUrl { get; set; }
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

        // These are used to group the models into a hierarchy
        public string[] Levels { get; set; }

        // Description of the Model as extracted from Yaml annotation files
        public string Description { get; set; }

        // Is this model Deprecated - as per Yaml annotation file
        public bool Deprecated { get; set; }

        // Associations
        public List<Column> AllColumns { get; internal set; }
        public List<Label> Labels = new List<Label>();      // Arbitrary user-defined labels
        public HierarchyItem LeafHierachyItem {get;set;}

        #region Re-Hydrated
        public Model Superclass { get; set; }
        public List<Model> DerivedClasses { get; set; }
        #endregion


        #region Derived
        public string HumanName { get { return NameUtils.ToHuman(Name); } }
        [JsonIgnore]
        public IEnumerable<Column> RegularColumns { get { return AllColumns.Where(x => !x.IsRef); } }
        [JsonIgnore]
        public IEnumerable<Column> RefColumns { get { return AllColumns.Where(x => x.IsRef); } }
        public string SanitizedQualifiedName { get { return FileUtils.SanitizeFilename(QualifiedName); } }
        public bool HasPolymorphicInterfaces { get { return PolymorphicInterfaces.Any(); } }
        public string ColorString { get; internal set; }

        [JsonIgnore]
        public List<Association> RefAssociations {
            get { return Schema.Singleton.RefAssociationsForModel(this); }
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

        #region Utilities

        // levelIndex is zero-based
        public string GetLevel(int levelIndex) {
            if (Levels == null || Levels.Length <= levelIndex)
                return null;
            return Levels[levelIndex];
        }

        // levelIndex is zero-based
        public void SetLevel(int levelIndex, string name) {
            if (Levels == null || Levels.Length < levelIndex)
                throw new Exception("Levels missing or too short");

            if (Levels.Length == levelIndex)
                Levels = Levels.Concat(new string[] { name }).ToArray();
            else
                Levels[levelIndex] = name;
        }

        public void AddLabel(string name, string value) {
            Labels.Add(new Label() {
                Name = name,
                Value = value,
            });
        }

        public void AddUrl(string name, string url) {
            Labels.Add(new Label() {
                Name = name,
                Value = url,
                IsUrl = true,
            });
        }

        public Column FindColumn(string dbColumnName, string dataType = null) {
            Column column = AllColumns.SingleOrDefault(x => x.Name.ToLower() == dbColumnName.ToLower());
            if (column == null)
                return null;

            if (dataType != null)
                return column.DataType == dataType ? column : null;

            return column;
        }

        public void RemoveColumn(string dbColumnName) {
            Column column = FindColumn(dbColumnName);
            if (column != null)
                AllColumns.Remove(column);
        }

        public IEnumerable<Model> SelfAndConnected() {
            HashSet<Model> models = new HashSet<Model>();
            SelfAndAllConnectedRecursive(models, this);
            return models;
        }

        private void SelfAndAllConnectedRecursive(HashSet<Model> models, Model model) {
            if (models.Contains(model))
                return;

            models.Add(model);

            foreach (Column column in model.RefColumns)
                SelfAndAllConnectedRecursive(models, column.ReferencedModel);

            if (model.Superclass != null)
                SelfAndAllConnectedRecursive(models, model.Superclass);

            foreach (Model derived in model.DerivedClasses) {
                SelfAndAllConnectedRecursive(models, derived);
            }
        }


        public override string ToString() {
            return Name;
        }
        #endregion
    }
}
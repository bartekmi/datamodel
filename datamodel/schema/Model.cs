using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

using datamodel.utils;
using datamodel.toplevel;

namespace datamodel.schema {
    public class Label {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsUrl { get; set; }

        public Label() { }

        public Label(string name, string value) {
            Name = name;
            Value = value;
        }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Model : IDbElement {
        // The fully-qualified Ruby Class-Name of the super-class of the Model
        public string SuperClassName { get; set; }

        // Is this model abstract, as indicated by 'self.abstract_class = true'
        public bool IsAbstract { get; set; }

        // If set, the only purpose of this Model is to serve as a "list" of this type
        // - if referenced from another model, the association will be converted to a many-association
        // - The "List" model itself will be dropped
        [JsonIgnore]
        public string ListSemanticsForType { get; set; }

        // Name of the Model
        // TODO: This should be derived from QualifiedName, but this could be a big job
        public string Name { get; set; }

        // This (possibly longer) name must be guaranteed to be globally unique
        public string QualifiedName { get; set; }

        // Some schemas - e.g. Swagger - have a version attached to each Model
        public string Version { get; set; }

        // TODO: This is an implementation convenience and does not belong here. Remove.
        [JsonIgnore]
        public string QualifiedNameLessVersion { get; set; }

        // These are used to group the models into a hierarchy
        private string[] _levels;
        public string[] Levels {
            get { return _levels; }
            set {
                _levels = value ?? new string[0];
            }
        }
        public bool ShouldSerializeLevels() { return _levels != null && _levels.Length > 0; }

        // Description of the Model as extracted from Yaml annotation files
        public string Description { get; set; }
        public bool ShouldSerializeDescription() { return !string.IsNullOrWhiteSpace(Description); }

        // Is this model Deprecated - as per Yaml annotation file
        public bool Deprecated { get; set; }

        // Associations
        public List<Property> AllProperties { get; internal set; } = new List<Property>();
        public bool ShouldSerializeAllProperties() { return AllProperties != null && AllProperties.Count > 0; }
        public List<Method> Methods { get; internal set; } = new List<Method>();
        public bool ShouldSerializeMethods() { return Methods != null && Methods.Count > 0; }
        // Arbitrary user-defined labels
        public List<Label> Labels { get; set; } = [];
        public bool ShouldSerializeLabels() { return Labels != null && Labels.Count > 0; }

        [JsonIgnore]
        public HierarchyItem LeafHierachyItem { get; set; }

        #region Re-Hydrated
        [JsonIgnore]
        public Model Superclass { get; set; }
        [JsonIgnore]
        public List<Model> DerivedClasses { get; set; }
        #endregion


        #region Derived
        [JsonIgnore]
        public IEnumerable<Member> AllMembers { get { return AllProperties.Cast<Member>().Concat(Methods); } }
        [JsonIgnore]
        public string HumanName { get { return NameUtils.ToHuman(Name); } }
        [JsonIgnore]
        public IEnumerable<Property> RegularProperties { get { return AllProperties.Where(x => !x.IsRef); } }
        [JsonIgnore]
        public IEnumerable<Property> RefProperties { get { return AllProperties.Where(x => x.IsRef); } }
        [JsonIgnore]
        public string SanitizedQualifiedName { get { return FileUtils.SanitizeFilename(QualifiedName); } }
        [JsonIgnore]
        public bool HasPolymorphicInterfaces { get { return PolymorphicInterfaces.Any(); } }
        [JsonIgnore]
        public bool HasRootLevel { get { return RootLevel != null; } }
        [JsonIgnore]
        public string RootLevel { get { return GetLevel(0); } }

        // Color String is assigned based on hierarchy
        private string _colorString;
        [JsonIgnore]
        public string ColorString {
            get { return ColorStringOverride ?? _colorString; }
            internal set { _colorString = value; }
        }

        // ColorStringOverride can be set at any point, including by schema sources. This will override any color assignment due to
        // hierarchy grouping.
        [JsonIgnore]
        public string ColorStringOverride { get; set; }

        public Model() {
            AllProperties = new List<Property>();
            Levels = new string[0];
        }

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

        public Property FindProperty(string propertyName, string dataType = null) {
            Property property = AllProperties.SingleOrDefault(x => x.Name.ToLower() == propertyName.ToLower());
            if (property == null)
                return null;

            if (dataType != null)
                return property.DataType == dataType ? property : null;

            return property;
        }

        public void RemoveProperty(string propertyName) {
            Property property = FindProperty(propertyName);
            if (property != null)
                AllProperties.Remove(property);
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

            foreach (Property property in model.RefProperties)
                SelfAndAllConnectedRecursive(models, property.ReferencedModel);

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
using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using datamodel.schema.source;

namespace datamodel.schema.tweaks {

    public class TempSource : SchemaSource {
        public string Title;
        public Dictionary<string, Model> Models = new Dictionary<string, Model>();
        public List<Association> Associations = new List<Association>();
        public bool ShouldSerializeAssociations() { return Associations.Count > 0; }

        // Note used, but necessary to appease abstract base class
        public override IEnumerable<Parameter> GetParameters() {
            return new List<Parameter>();
        }
        public override void Initialize(Parameters parameters) {}

        // Derived
        [JsonIgnore]
        public IEnumerable<Property> AllProperties {
            get {
                return Models.Values.SelectMany(x => x.AllProperties);
            }
        }

        public override string GetTitle() {
            return Title;
        }
        public override IEnumerable<Model> GetModels() {
            return Models.Values;
        }
        public override IEnumerable<Association> GetAssociations() {
            return Associations;
        }

        // Return an association which is exactly like 'likeThis', but for the specified owner
        internal Association FindOwnedAssociation(string owner, Association likeThis) {
            foreach (Association association in Associations) {
                if (association.OwnerSide == owner &&
                    association.OwnerMultiplicity == likeThis.OwnerMultiplicity &&
                    association.OtherSide == likeThis.OtherSide &&
                    association.OtherMultiplicity == likeThis.OtherMultiplicity &&
                    association.OtherRole == likeThis.OtherRole)
                    return association;
            }
            return null;
        }

        // Return an association which is exactly like 'likeThis', but for the specified owner
        internal Association FindIncomingAssociation(string incoming, Association likeThis) {
            foreach (Association association in Associations) {
                if (association.OtherSide == incoming &&
                    association.OtherMultiplicity == likeThis.OtherMultiplicity &&
                    association.OwnerMultiplicity == likeThis.OwnerMultiplicity &&
                    association.OwnerSide == likeThis.OwnerSide)
                    return association;
            }
            return null;
        }

        internal Model GetModel(string qualifiedName) {
            Model model = FindModel(qualifiedName);
            if (model == null)
                throw new Exception("Not a valid model: " + qualifiedName);
            return model;
        }

        internal Model FindModel(string qualifiedName) {
            Models.TryGetValue(qualifiedName, out Model model);
            return model;
        }

        internal static TempSource CloneFromSource(SchemaSource source) {
            TempSource clone = new TempSource();

            clone.Title = source.GetTitle();
            clone.SetModels(source.GetModels());
            clone.Associations = new List<Association>(source.GetAssociations());

            return clone;
        }

        public void SetModels(IEnumerable<Model> models) {
             IEnumerable<string> duplicateNames = models.Select(x => x.QualifiedName)
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);

            if (duplicateNames.Count() > 0)
                throw new Exception("Duplicate Model qualified names: " + string.Join(", ", duplicateNames));
            
            Models = models.ToDictionary(x => x.QualifiedName);
        }

        public void AddModel(Model model) {
            if (Models.ContainsKey(model.QualifiedName))
                throw new Exception("Model already exists: " + model.QualifiedName);
            Models[model.QualifiedName] = model;
        }

        public void RenameModel(Model model, string newQualifiedName, string newName) {
            foreach (Association assoc in Associations) {
                if (assoc.OwnerSide == model.QualifiedName)
                    assoc.OwnerSide = newQualifiedName;
                    
                if (assoc.OtherSide == model.QualifiedName)
                    assoc.OtherSide = newQualifiedName;
            }

            model.QualifiedName = newQualifiedName;
            model.Name = newName;

            Models.Remove(model.QualifiedName);
            AddModel(model);

        }

        public void RemoveModel(Model model) {
            Models.Remove(model.QualifiedName);
        }

        public void RemoveAssociation(Association assoc) {
            Associations.Remove(assoc);
        }

        #region Helpful for testing
        internal void RemovePropertyLabels() {
            foreach (Property property in AllProperties)
                property.Labels = null;   // Clean up output
        }

        internal string ToJasonNoQuotes(bool removePropertyLabels = true) {
            if (removePropertyLabels)
                RemovePropertyLabels();

            string json = JsonConvert.SerializeObject(
                this,
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    Formatting = Formatting.Indented,
                    Converters = new List<JsonConverter>() { new StringEnumConverter()},
                });

            // Quotes are a pain because the make it hard to copy-and-paste results as the 
            // "expected" string
            json = json.Replace("\"", "");

            return json.Trim();
        }
        #endregion
    }
}
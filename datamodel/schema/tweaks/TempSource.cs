using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.source;

namespace datamodel.schema.tweaks {

    public class TempSource : SchemaSource {
        internal string _title;
        internal Dictionary<string, Model> Models;
        internal List<Association> Associations;

        public override string GetTitle() {
            return _title;
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
            clone._title = source.GetTitle();
            clone.SetModels(source.GetModels());
            clone.Models = source.GetModels().ToDictionary(x => x.QualifiedName);
            clone.Associations = new List<Association>(source.GetAssociations());
            return clone;
        }

        public void SetModels(IEnumerable<Model> models) {
            Models = models.ToDictionary(x => x.QualifiedName);
        }

        public void AddModel(Model model) {
            if (Models.ContainsKey(model.QualifiedName))
                throw new Exception("Model already exists: " + model.QualifiedName);
            Models[model.QualifiedName] = model;
        }

        public void RemoveAssociation(Association assoc) {
            Associations.Remove(assoc);
        }
    }
}
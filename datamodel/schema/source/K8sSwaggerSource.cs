using System;
using System.Collections.Generic;
using System.Linq;

namespace datamodel.schema.source {
    public class K8sSwaggerSource : SwaggerSource {
        public K8sSwaggerSource(string json, SwaggerSourceOptions options) : base(json, options) {
            // Do nothing
        }

        protected override void PopulateModel(Model model, string qualifiedName, SwgDefinition def) {
            base.PopulateModel(model, qualifiedName, def);

            // In almost all cases, the second-last element of the qualified name is
            // the API version of the entity. Set version info on the models.
            string[] pieces = qualifiedName.Split(".");
            if (pieces.Length >= 2) {
                string version = pieces.Reverse().Skip(1).First();
                if (version.ToLower().StartsWith("v")) {
                    model.Version = version;
                    var piecesLessVersion = pieces.Where(x => x != version);
                    model.QualifiedNameLessVersion = string.Join(".", piecesLessVersion);
                }
            }

            Association assoc = _associations.SingleOrDefault(x => x.OwnerSide == model.QualifiedName && x.OtherRole == "items");
            if (model.Name.EndsWith("List") && assoc != null)
                model.ListSemanticsForType = assoc.OtherSide;
        }

        protected override IEnumerable<Model> FilterModels(IEnumerable<Model> models) {
            List<Model> filtered = new List<Model>();

            // Only take the latest of multiple versioned models 
            foreach (var group in models.GroupBy(x => x.QualifiedNameLessVersion)) {
                Model latest = group.OrderBy(x => x.Version, new VersionComparer()).Last();
                filtered.Add(latest);
            }

            return filtered;
        }

        public override void PostProcessSchema() {
            K8sToc.AssignCoreLevel2Groups();
        }

        // Format is expected to be one of:
        // vX
        // vXbetaY
        internal class VersionComparer : IComparer<string> {
            public int Compare(string a, string b) {
                if (a == b)
                    return 0;

                char va = a[1];
                char vb = b[1];

                if (va == vb) {
                    bool isBetaA = a.Contains("beta");
                    bool isBetaB = b.Contains("beta");

                    if (isBetaA && !isBetaB)
                        return -1;
                    if (!isBetaA && isBetaB)
                        return +1;

                    char betaA = a.Last();
                    char betaB = b.Last();
                    return betaA.CompareTo(betaB);
                } else
                    return va.CompareTo(vb);
            }
        }
    }
}
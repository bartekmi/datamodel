using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.tweaks;

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

                    model.AddLabel("Version", version);
                }
            }

            Association assoc = _associations.SingleOrDefault(x => x.OwnerSide == model.QualifiedName && x.OtherRole == "items");
            if (model.Name.EndsWith("List") && assoc != null)
                model.ListSemanticsForType = assoc.OtherSide;
        }
    }

    public class FilterOldApiVersionsTweak : FilterModelsTweak {
        public override IEnumerable<Model> ModelsToFilterOut(TempSource source) {
            List<Model> toRemove = new List<Model>();

            // Only take the latest of multiple versioned models 
            foreach (var group in source.GetModels().GroupBy(x => x.QualifiedNameLessVersion)) {
                if (group.Key != null && group.Count() > 1) {
                    var byVersion = group.OrderBy(x => x.Version, new VersionComparer());
                    toRemove.AddRange(byVersion.Take(byVersion.Count() - 1));
                }
            }

            return toRemove;
        }

        internal class VersionComparer : IComparer<string> {
            public int Compare(string a, string b) {
                int aInt = VersionToInt(a);
                int bInt = VersionToInt(b);

                return aInt.CompareTo(bInt);
            }

            // Format is expected to be one of:
            // vX
            // vXbetaY
            // vXalphaY
            private int VersionToInt(string version) {
                int versionContribution = (version[1] - '0') * 1000;
                int alphaBetaContribution = version.Length == 2 ?
                    999 : (version[2] - 'a') * 100;
                int minorVersionContribution = version.Last() - '0';

                return
                    versionContribution +
                    alphaBetaContribution +
                    minorVersionContribution;
            }
        }
    }
}
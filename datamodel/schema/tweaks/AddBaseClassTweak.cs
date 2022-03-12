using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace datamodel.schema.tweaks {
    public class AddBaseClassTweak : Tweak {
        // UN-qualified name of the newly created Base Class
        public string BaseClassName;

        // Optional description of the base class
        public string BaseClassDescription;

        // List of fully-qualified names of the models which should be "derived" from the 
        // newly created base model
        public string[] DerviedQualifiedNames;

        // If true, in addition to promoting properties and owned/outgoing associations,
        // incoming associations will also be promoted.
        // the reason this is optional is because of the possible resulting information loss
        // (See comments below)
        public bool PromoteIncomingAssociations;

        public override void Apply(TempSource source) {
            string baseClassQN = ComputeBaseClassName();

            List<Model> models = DerviedQualifiedNames
                .Select(x => source.GetModel(x))
                .ToList();

            int maxCommonLevels = models.Max(x => x.Levels.Length);
            string[] levels = models.First().Levels.Take(maxCommonLevels).ToArray();

            Model baseClass = new Model() {
                Name = BaseClassName,
                QualifiedName = baseClassQN,
                Description = ComputeDescription(),
                IsAbstract = true,
                AllColumns = new List<Column>(),
                Levels = levels,
            };
            source.AddModel(baseClass);


            foreach (Model model in models)
                model.SuperClassName = baseClassQN;

            PromoteProperties(baseClass, models);
            PromoteOwnedAssociations(source, baseClass, models);

            if (PromoteIncomingAssociations)
                DoPromoteIncomingAssociations(source, baseClass, models);
        }

        private void PromoteProperties(Model baseClass, IEnumerable<Model> models) {
            Model first = models.First();

            // Iterate props of first... See if present in all others
            // If yes, add to base class and remove from all derived
            foreach (Column propInFirst in first.AllColumns.ToList()) {
                bool foundInAllPeers = true;
                foreach (Model peer in models.Skip(1)) {
                    Column prop = peer.FindColumn(propInFirst.Name, propInFirst.DataType);
                    if (prop == null) {
                        foundInAllPeers = false;
                        break;
                    }
                }

                if (foundInAllPeers) {
                    // All peer models have this property... Promote to base class
                    // Note that it is possible that we lose information, since we arbitrarily use the description
                    // of the property from the first instance of the derived class 
                    baseClass.AllColumns.Add(propInFirst);
                    foreach (Model model in models)
                        model.RemoveColumn(propInFirst.Name);
                }
            }
        }

        private void PromoteOwnedAssociations(TempSource source, Model baseClass, IEnumerable<Model> models) {
            Model first = models.First();

            IEnumerable<Association> assocsInFirst = source.Associations
                .Where(x => x.OwnerSide == first.QualifiedName)
                .ToList();

            // Iterate associations of first... See if present in all others
            // If yes, add to base class and remove from all derived
            foreach (Association assocInFirst in assocsInFirst) {
                List<Association> peerAssociations = new List<Association>();
                bool foundInAllPeers = true;
                foreach (Model peer in models.Skip(1)) {
                    Association assoc = source.FindOwnedAssociation(peer.QualifiedName, assocInFirst);
                    if (assoc == null) {
                        foundInAllPeers = false;
                        break;
                    }
                    peerAssociations.Add(assoc);
                }

                if (foundInAllPeers) {
                    // All peer models have this association... Promote to base class
                    // Note that it is possible that we lose information, since we arbitrarily use the description
                    // of the association from the first instance of the derived class 
                    assocInFirst.OwnerSide = baseClass.QualifiedName;   // "Donate" assoc of first model to the base class
                    foreach (Association assoc in peerAssociations)     // Remove eeryone else's
                        source.RemoveAssociation(assoc);
                }
            }
        }

        private void DoPromoteIncomingAssociations(TempSource source, Model baseClass, IEnumerable<Model> models) {
            Model first = models.First();

            IEnumerable<Association> assocsToFirst = source.Associations
                .Where(x => x.OtherSide == first.QualifiedName)
                .ToList();

            // Iterate associations coming into first... See if they are present in all others
            // If yes, add to base class and remove from all derived
            foreach (Association assocToFirst in assocsToFirst) {
                List<Association> peerAssociations = new List<Association>();
                bool foundInAllPeers = true;
                foreach (Model peer in models.Skip(1)) {
                    Association assoc = source.FindIncomingAssociation(peer.QualifiedName, assocToFirst);
                    if (assoc == null) {
                        foundInAllPeers = false;
                        break;
                    }
                    peerAssociations.Add(assoc);
                }

                if (foundInAllPeers) {
                    // All peer models have this association... Promote to base class
                    // Note that it is possible that we lose information, since we arbitrarily use the description
                    // of the association from the first instance of the derived class 
                    assocToFirst.OtherSide = baseClass.QualifiedName;   // "Donate" assoc to first to the base class
                    assocToFirst.OtherRole = null;                      // Role no longer specific to first, so blank it out
                    foreach (Association assoc in peerAssociations)     // Remove everyone else's
                        source.RemoveAssociation(assoc);
                }
            }
        }

        private string ComputeBaseClassName() {
            List<string[]> piecesForDerived = DerviedQualifiedNames.Select(x => x.Split('.')).ToList();
            List<string> common = new List<string>();

            // Keep iterating while all pieces[index] are the same and not the last in the chain
            for (int index = 0; true; index++) {
                string current = null;

                // Iterate over all derived classes
                for (int ii = 0; ii < piecesForDerived.Count; ii++) {
                    string[] pieces = piecesForDerived[ii];
                    if (index >= pieces.Length - 1)   // Break out if current piece is last (i.e. the name)
                        goto BreakOut;

                    string piece = pieces[index];
                    if (ii == 0)
                        current = piece;
                    else if (piece != current)      // Piece is different... Break out of nested loop
                        goto BreakOut;
                }

                // This name "piece" non-last AND identical for all derived classes... Add to base class
                common.Add(current);
            }

        BreakOut:
            common.Add(BaseClassName);
            return string.Join(".", common);
        }

        private string ComputeDescription() {
            StringBuilder builder = new StringBuilder();

            if (BaseClassDescription != null) {
                builder.AppendLine(BaseClassDescription);
                builder.AppendLine();
            }

            builder.Append("This class was added artificially to simplify the diagram");

            return builder.ToString();
        }
    }
}
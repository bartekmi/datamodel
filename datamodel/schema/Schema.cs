using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.source;

namespace datamodel.schema {
    public class Schema {

        #region Properties and Constructor

        public string Title { get; private set; }
        public string Level1 { get; set; }
        public string Level2 { get; set; }
        public string Level3 { get; set; }
        public string[] BoringProperties { get; set; }

        public HashSet<Model> Models { get; private set; }
        public List<Association> Associations { get; private set; }
        public Dictionary<string, PolymorphicInterface> Interfaces { get; private set; }

        private Dictionary<string, Model> _byQualifiedName;
        private Dictionary<Model, List<Column>> _incomingRefColumns;
        private Dictionary<Model, List<Association>> _refAssociationsForModel;
        private Dictionary<Model, List<PolymorphicInterface>> _interfacesForModel;
        private Dictionary<PolymorphicInterface, List<Association>> _polymorphicAssociations;

        private Schema() {
            Interfaces = new Dictionary<string, PolymorphicInterface>();
        }

        #endregion

        #region Creation
        private static Schema _schema;
        public static Schema Singleton {
            get {
                if (_schema == null)
                    throw new Exception("Call CreateSchema() first");
                return _schema;
            }
        }

        // Create the Singleton schema. Normally, it is then accessed by 'Schema.Singleton'
        public static Schema CreateSchema(SchemaSource rawSource) {
            SchemaSource source = rawSource.ApplyPreHydrationTweaks();

            // TODO: As best as I can tell, this code converts: object<>----List----<Item into object<>--->Items
            // if used at all, it belongs in Tweaks
            HashSet<Model> models = new HashSet<Model>(source.GetModels());
            var assocs = source.GetAssociations()
                .GroupBy(x => x.OtherSide)
                .ToDictionary(x => x.Key, x => (IEnumerable<Association>)x);

            foreach (Model model in models.Where(x => x.ListSemanticsForType != null).ToList()) {
                if (assocs.TryGetValue(model.QualifiedName, out IEnumerable<Association> incoming)) {
                    foreach (Association association in incoming) {
                        if (association.OtherMultiplicity == Multiplicity.Many) {
                            throw new NotImplementedException("Encountered many-association to a List");
                        }
                        association.OtherMultiplicity = Multiplicity.Many;
                        association.OtherSide = model.ListSemanticsForType;
                        // TODO... to be perfect, we should also discard list to item associtation
                    }
                }

                models.Remove(model);
            }

            _schema = new Schema() {
                Title = source.GetTitle(),
                Models = models,
                Associations = source.GetAssociations().ToList(),

                // Some default values, but these can be changed
                Level1 = "Level1",
                Level2 = "Level2",
                Level3 = "Level3",
            };

            _schema._byQualifiedName = _schema.Models.ToDictionary(x => x.QualifiedName);
            _schema.CreateRefColumns();

            _schema.Rehydrate();
            rawSource.ApplyPostHydrationTweaks();

            return _schema;
        }

        private void CreateRefColumns() {
            foreach (Association assoc in Associations) {
                if (!_byQualifiedName.TryGetValue(assoc.OwnerSide, out Model refModel))
                    Error.Log("Association refers to unknown model: {0}", assoc.OwnerSide);

                if (!_byQualifiedName.TryGetValue(assoc.OtherSide, out Model otherModel))
                    Error.Log("Association refers to unknown model: {0}", assoc.OtherSide);

                if (refModel == null || otherModel == null)
                    continue;

                Column refColumn = new Column() {
                    Name = assoc.OtherRole ?? assoc.OtherSide,
                    Description = assoc.Description,
                    DataType = "ID",
                    CanBeEmpty = assoc.OtherMultiplicity == Multiplicity.ZeroOrOne,
                    ReferencedModel = otherModel,
                };

                refModel.AllColumns.Add(refColumn);
                assoc.RefColumn = refColumn;
            }
        }
        #endregion

        #region Rehydrate

        private void Rehydrate() {
            RehydrateColumnOwner();
            RehydrateSuperAndDerivedClasses();
            RemoveDuplicatePolymorphicInterfaces();

            RehydrateModelsOnAssociations();
            RehydrateIncomingAssociations();
            RehydrateInterfacesForModels();
            RehydratePolymorphicRefColumns();
            RehydratePolymorphicAssociations();
            RehydrateRefAssociationsForModels();
        }

        private void RehydrateColumnOwner() {
            foreach (Model model in Models) {
                foreach (Column column in model.AllColumns)
                    column.Owner = model;
            }
        }

        // E.g. see OperationalRoute::Graph and OperationalRoute::ConfirmedGraph
        // The models both have a polymorphic interface, but they really refer to the same thing
        private void RemoveDuplicatePolymorphicInterfaces() {
            foreach (var keyValue in new Dictionary<string, PolymorphicInterface>(Interfaces)) {        // Clone to allow remove while iterating
                Column column = keyValue.Value.Column;
                Model superclass = keyValue.Value.Model.Superclass;

                // Too lazy to make recursive
                while (superclass != null) {
                    if (superclass.FindColumn(column.Name) != null) {
                        Interfaces.Remove(keyValue.Key);
                        Error.Log("Removing duplicate Polymorphic Interface: " + keyValue.Key);
                        break;
                    }
                    superclass = superclass.Superclass;
                }
            }
        }

        private void RehydratePolymorphicRefColumns() {
            foreach (PolymorphicInterface _interface in Interfaces.Values) {
                _interface.Column.IsPolymorphicId = true;
                Model model = _interface.Column.Owner;
                string idColumnName = _interface.Column.Name;
                string typeColumnName = idColumnName
                    .Substring(0, idColumnName.Length - "_id".Length)
                    + "_type";
                Column typeColumn = model.FindColumn(typeColumnName);
                typeColumn.IsPolymorphicType = true;
            }
        }

        private void RehydratePolymorphicAssociations() {
            _polymorphicAssociations = Associations
              .Where(x => x.IsPolymorphic)
              .GroupBy(x => x.PolymorphicName)
              .Where(x => Interfaces.ContainsKey(x.Key))
              .ToDictionary(x => Interfaces[x.Key], x => x.ToList());

            // Set the OwnerSideModel for the polymorphic associations 
            foreach (Association association in Associations.Where(x => x.IsPolymorphic)) {
                if (Interfaces.TryGetValue(association.PolymorphicName, out PolymorphicInterface _interface)) {
                    Column refColumn = _interface.Column;
                    if (refColumn != null)
                        association.OwnerSideModel = refColumn.Owner;
                    else
                        Error.Log("WARNING: Ref Column null for " + association);
                }
            }

            Console.WriteLine("Filtered out: " +
              string.Join("\n", Associations
                  .Where(x => x.IsPolymorphic)
                  .Select(x => x.PolymorphicName)
                  .Except(_polymorphicAssociations.Keys.Select(x => x.Name))));

            Console.WriteLine("\nInterfaces: " + string.Join("\n", Interfaces.Keys));

            Console.WriteLine("\nPAs: " + string.Join("\n", _polymorphicAssociations.Keys.Select(x => x.Name)));
        }

        private void RehydrateInterfacesForModels() {
            _interfacesForModel = Interfaces.Values
                .GroupBy(x => x.Model)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        private void RehydrateRefAssociationsForModels() {
            _refAssociationsForModel = Associations
                .Where(x => x.OwnerSideModel != null)
                .GroupBy(x => x.OwnerSideModel)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        private void RehydrateIncomingAssociations() {
            _incomingRefColumns = Models
                .SelectMany(x => x.RefColumns)
                .GroupBy(x => x.ReferencedModel)
                .Where(x => x.Key != null)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        private void RehydrateSuperAndDerivedClasses() {
            foreach (Model model in Models) 
                model.DerivedClasses = new List<Model>();

            foreach (Model model in Models) {
                if (model.SuperClassName != null)
                    if (_byQualifiedName.TryGetValue(model.SuperClassName, out Model parent)) {
                        // Hydrate super/derived in both directions
                        model.Superclass = parent;
                        parent.DerivedClasses.Add(model);

                        // Remove duplicate column definitions
                        foreach (Column column in parent.AllColumns) {
                            Column duplicate = model.FindColumn(column.Name);
                            if (duplicate != null)
                                model.AllColumns.Remove(duplicate);
                        }
                    }
            }
        }

        private void RehydrateModelsOnAssociations() {
            foreach (Association association in Associations) {
                if (_byQualifiedName.TryGetValue(association.OtherSide, out Model otherSideModel))
                    association.OtherSideModel = otherSideModel;
                if (_byQualifiedName.TryGetValue(association.OwnerSide, out Model ownerSideModel))
                    association.OwnerSideModel = ownerSideModel;
            }
        }
        #endregion

        #region Utility Methods

        public bool IsInteresting(Column column) {
            if (BoringProperties == null)
                return true;

            return BoringProperties.Contains(column.Name) ? false : true;
        }

        public Model FindByQualifiedName(string qualifiedName) {
            if (_byQualifiedName.TryGetValue(qualifiedName, out Model table))
                return table;
            return null;
        }

        public IEnumerable<Column> IncomingRefColumns(Model model) {
            if (_incomingRefColumns.TryGetValue(model, out List<Column> columns))
                return columns;
            return new Column[0];
        }

        public IEnumerable<PolymorphicInterface> InterfacesForModel(Model model) {
            if (_interfacesForModel.TryGetValue(model, out List<PolymorphicInterface> interfaces))
                return interfaces;
            return new PolymorphicInterface[0];
        }

        public List<Association> RefAssociationsForModel(Model model) {
            if (_refAssociationsForModel.TryGetValue(model, out List<Association> refAssociations))
                return refAssociations;
            return new List<Association>();
        }

        public IEnumerable<Association> PolymorphicAssociationsForInterface(PolymorphicInterface _interface) {
            if (!_polymorphicAssociations.TryGetValue(_interface, out List<Association> associations))
                return new Association[0];
            return associations;
        }

        #endregion
    }
}
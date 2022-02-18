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

        public List<Model> Models { get; private set; }
        public List<Association> Associations { get; private set; }
        public Dictionary<string, PolymorphicInterface> Interfaces { get; private set; }

        private Dictionary<string, Model> _byClassName;
        private Dictionary<Model, List<Column>> _incomingFkColumns;
        private Dictionary<Model, List<Association>> _fkAssociationsForModel;
        private Dictionary<Model, List<PolymorphicInterface>> _interfacesForModel;
        private Dictionary<PolymorphicInterface, List<Association>> _polymorphicAssociations;
        private HashSet<string> _unqualifiedClassNames;

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
        public static Schema CreateSchema(SchemaSource source) {
            _schema = new Schema() {
                Title = source.GetTitle(),
                Models = source.GetModels().ToList(),
                Associations = source.GetAssociations().ToList(),

                // Some default values, but these can be changed
                Level1 = "Level1",
                Level2 = "Level2",
                Level3 = "Level3",
            };

            _schema._byClassName = _schema.Models.ToDictionary(x => x.FullyQualifiedName);
            _schema.CreateFkColumns();

            _schema.Rehydrate();

            return _schema;
        }

        private void CreateFkColumns() {
            foreach (Association assoc in Associations) {
                if (!_byClassName.TryGetValue(assoc.OwnerSide, out Model fkModel)) 
                    Error.Log("Association refers to unknown model: {0}", assoc.OwnerSide);

                if (!_byClassName.TryGetValue(assoc.OtherSide, out Model otherModel)) 
                    Error.Log("Association refers to unknown model: {0}", assoc.OtherSide);

                if (fkModel == null || otherModel == null)
                    continue;

                Column fkColumn = new Column(fkModel) {
                    Name = assoc.OtherRole,
                    DataType = "ID",
                    CanBeEmpty = assoc.OtherMultiplicity == Multiplicity.ZeroOrOne,
                    FkInfo = new FkInfo() {
                        ReferencedModel = otherModel,
                    },
                };

                fkModel.AllColumns.Add(fkColumn);
                assoc.FkColumn = fkColumn;
            }
        }
        #endregion

        #region Rehydrate

        private void Rehydrate() {
            RehydrateSuperClasses();
            RemoveDuplicatePolymorphicInterfaces();

            RehydrateModelsOnAssociations();
            RehydrateIncomingAssociations();
            RehydrateInterfacesForModels();
            RehydratePolymorphicFkColumns();
            RehydratePolymorphicAssociations();
            RehydrateFkAssociationsForModels();
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

        private void RehydratePolymorphicFkColumns() {
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

            // Set the FkSideModel for the polymorphic associations +++
            foreach (Association association in Associations.Where(x => x.IsPolymorphic)) {
                if (Interfaces.TryGetValue(association.PolymorphicName, out PolymorphicInterface _interface)) {
                    Column fkColumn = _interface.Column;
                    if (fkColumn != null)
                        association.OwnerSideModel = fkColumn.Owner;
                    else
                        Error.Log("WARNING: FK Column null for " + association);
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

        private void RehydrateFkAssociationsForModels() {
            _fkAssociationsForModel = Associations
                .Where(x => x.OwnerSideModel != null)
                .GroupBy(x => x.OwnerSideModel)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        private void RehydrateIncomingAssociations() {
            _incomingFkColumns = Models
                .SelectMany(x => x.FkColumns)
                .GroupBy(x => x.FkInfo.ReferencedModel)
                .Where(x => x.Key != null)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        private void RehydrateSuperClasses() {
            foreach (Model table in Models) {
                if (table.SuperClassName != null)
                    if (_byClassName.TryGetValue(table.SuperClassName, out Model parent)) {
                        table.Superclass = parent;
                        foreach (Column column in parent.AllColumns) {
                            Column duplicate = table.FindColumn(column.Name);
                            if (duplicate != null)
                                table.AllColumns.Remove(duplicate);
                        }
                    }
            }
        }

        private void RehydrateModelsOnAssociations() {
            foreach (Association association in Associations) {
                if (_byClassName.TryGetValue(association.OtherSide, out Model otherSideModel))
                    association.OtherSideModel = otherSideModel;
                if (_byClassName.TryGetValue(association.OwnerSide, out Model fkSideModel))
                    association.OwnerSideModel = fkSideModel;
            }
        }
        #endregion

        #region Utility Methods

        public bool IsInteresting(Column column) {
            if (BoringProperties == null)
                return true;

            return BoringProperties.Contains(column.Name) ? false : true;
        }

        public Model FindByClassName(string className) {
            if (_byClassName.TryGetValue(className, out Model table))
                return table;
            return null;
        }

        public IEnumerable<Column> IncomingFkColumns(Model model) {
            if (_incomingFkColumns.TryGetValue(model, out List<Column> columns))
                return columns;
            return new Column[0];
        }

        public IEnumerable<PolymorphicInterface> InterfacesForModel(Model model) {
            if (_interfacesForModel.TryGetValue(model, out List<PolymorphicInterface> interfaces))
                return interfaces;
            return new PolymorphicInterface[0];
        }

        public List<Association> FkAssociationsForModel(Model model) {
            if (_fkAssociationsForModel.TryGetValue(model, out List<Association> fkAssociations))
                return fkAssociations;
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
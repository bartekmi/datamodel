using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.source;

namespace datamodel.schema {
    public class Schema {

        #region Properties and Constructor

        // These columns automatically added by Rails and are of no interest
        private static readonly string[] UNINTERESTING_COLUMNS = new string[] {
            "id",
            "created_at",
            "updated_at",
            "deleted_at",
            "archived_at",
            "lock_version"
        };

        // This is a singleton
        private static Schema _schema;
        public static Schema Singleton {
            get {
                return _schema;
            }
        }
        public static void CreateSchema(SchemaSource source) {
            _schema = new Schema() {
                Models = source.GetModels().ToList(),
                Associations = source.GetAssociations().ToList(),
            };

            _schema._byClassName = _schema.Models.ToDictionary(x => x.ClassName);
            _schema.CreateFkColumns();

            _schema.Rehydrate();
        }

        private void CreateFkColumns() {
            foreach (Association assoc in Associations) {
                // TODO: Validate unknown models in associations
                if (!_byClassName.TryGetValue(assoc.FkSide, out Model aModel) ||
                    !_byClassName.TryGetValue(assoc.OtherSide, out Model bModel)) 
                    continue;

                aModel.AllColumns.Add(new Column(aModel) {
                    DbName = bModel.DbName,
                    DbType = DataType.Integer,
                    IsNull = assoc.OtherSideMultiplicity == Multiplicity.ZeroOrOne,
                    FkInfo = new FkInfo() {
                        ReferencedModel = bModel,
                    },
                });
            }
        }


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
        private static Schema ParseSchema() {

            // Step One: Entities
            YamlSchemaParser parser = new YamlSchemaParser();
            List<Model> tables = parser.ParseModels();
            tables = tables.Where(x => !x.ClassName.Contains("HABTM")).ToList();        // Ignore dummy tables for "Has And Belongs To Many"

            Schema schema = new Schema();

            // Step Two: Associations
            IEnumerable<RailsAssociation> railsAssociations = parser.ParseAssociations();
            railsAssociations = railsAssociations.Where(x => !x.OwningModel.Contains("HABTM")).ToList();        // Ignore dummy associations for "Has And Belongs To Many"
            railsAssociations = new HashSet<RailsAssociation>(railsAssociations);   // Make unique
            railsAssociations = schema.SetFkModelsAndClean(railsAssociations);

            schema.Associations = BuildAssociations(railsAssociations)
                .Concat(BuildPolymorphicAssociations(railsAssociations))
                .ToList();

            schema.BuildPolymorphicInterfaces(railsAssociations);

            schema.Rehydrate();

            return schema;
        }

        # region Polymorphic Interfaces
        private void BuildPolymorphicInterfaces(IEnumerable<RailsAssociation> railsAssociations) {
            foreach (RailsAssociation railsAssoc in railsAssociations) {
                if (railsAssoc.IsPolymorphicInterface) {
                    PolymorphicInterface _interface = new PolymorphicInterface(railsAssoc);
                    Interfaces[_interface.Name] = _interface;
                }
            }
        }

        private static List<Association> BuildPolymorphicAssociations(IEnumerable<RailsAssociation> railsAssociations) {
            List<Association> associations = new List<Association>();

            foreach (RailsAssociation railsAssoc in railsAssociations) {
                if (railsAssoc.IsPolymorphicAssociation) {
                    associations.Add(new Association(railsAssoc, null) {
                        FkSide = railsAssoc.Options.As,
                        FkSideMultiplicity = DetermineFkSideMultiplicity(railsAssoc),
                        OtherSide = railsAssoc.OwningModel,
                        OtherSideMultiplicity = Multiplicity.ZeroOrOne,
                    });
                }
            }

            return associations;
        }
        #endregion

        #region Associations

        private static List<Association> BuildAssociations(IEnumerable<RailsAssociation> railsAssociations) {
            Dictionary<string, RailsAssociation> reverseAssociationDict = new Dictionary<string, RailsAssociation>();
            foreach (RailsAssociation ra in railsAssociations.Where(x => x.IsReverse)) {
                string key = MakeKey(ra, false);
                if (reverseAssociationDict.ContainsKey(key))
                    Error.Log("Duplicate association: " + key);
                else
                    reverseAssociationDict[key] = ra;
            }

            List<Association> associations = new List<Association>();
            foreach (RailsAssociation railsAssoc in railsAssociations) {
                if (railsAssoc.IsReverse ||                 // Avoid duplicates (i.e. fwd and rev only create on Association)
                    railsAssoc.IsPolymorphicInterface ||    // These are handled separately
                    railsAssoc.IsHABTM)                     // These don't seem to contribute anything
                    continue;

                reverseAssociationDict.TryGetValue(MakeKey(railsAssoc, true), out RailsAssociation reverseAssoc);

                associations.Add(new Association(railsAssoc, reverseAssoc) {
                    FkSide = railsAssoc.OwningModel,
                    FkSideMultiplicity = DetermineFkSideMultiplicity(reverseAssoc),
                    OtherSide = railsAssoc.OtherModel,
                    OtherSideMultiplicity = DetermineOtherSideMultiplicity(railsAssoc, reverseAssoc),
                });
            }

            return associations;
        }

        private static string MakeKey(RailsAssociation association, bool forward) {
            // There is a special case for HasAndBelongsToMany between same table
            // (There is a couple of them on CompanyEntity)
            if (association.Kind == AssociationKind.HasAndBelongsToMany &&
                association.OwningModel == association.OtherModel) {

                string source = forward ? association.ForeignKey : association.InverseOf;
                string destination = forward ? association.InverseOf : association.ForeignKey;

                return string.Format("{0}|{1}", source, destination);
            } else {
                string source = forward ? association.OwningModel : association.OtherModel;
                string destination = forward ? association.OtherModel : association.OwningModel;

                return string.Format("{0}|{1}|{2}", source, destination, association.ForeignKey);
            }
        }

        #region Multiplicity
        private static Multiplicity DetermineFkSideMultiplicity(RailsAssociation reverseAssociation) {
            // If there is no reverse association, by default, we assume that many entities
            // with the FK can point to the same entity
            if (reverseAssociation == null)
                return Multiplicity.Many;

            switch (reverseAssociation.Kind) {
                case AssociationKind.HasOne:
                    // I think that, in principle, this could also be ZeroOrOne, but can I tell?
                    // Can any information be garnered from the presence of 'dependent: :destroy' option?
                    return Multiplicity.ZeroOrOne;
                case AssociationKind.HasMany:
                    return Multiplicity.Many;
                case AssociationKind.HasAndBelongsToMany:
                    return Multiplicity.Many;
                default:
                    throw new Exception("Unexpected Kind: " + reverseAssociation.Kind);
            }
        }

        private static Multiplicity DetermineOtherSideMultiplicity(
            RailsAssociation forwardAssociation,
            RailsAssociation reverseAssociation
        ) {
            if (forwardAssociation.Kind == AssociationKind.HasAndBelongsToMany)
                return Multiplicity.Many;

            if (forwardAssociation.IsPolymorphicInterface)
                // In theory, we could take this from the FkColumn.IsMandatory
                // Which is better? I seriously doubt there is a use-case for things with
                // a polymorphic association that can live independently.
                return Multiplicity.One;

            if (forwardAssociation.FkColumn.IsMandatory)
                if (reverseAssociation != null && reverseAssociation.Options.Destroy)
                    return Multiplicity.Aggregation;
                else
                    return Multiplicity.One;

            return Multiplicity.ZeroOrOne;
        }
        #endregion
        #endregion
        #endregion

        #region Rehydrate

        private List<RailsAssociation> SetFkModelsAndClean(IEnumerable<RailsAssociation> dirties) {
            List<RailsAssociation> cleans = new List<RailsAssociation>();

            foreach (RailsAssociation ra in dirties) {
                // Through associations, by definition, are redundant data and not shown 
                if (ra.Kind == AssociationKind.Through)
                    continue;

                if (ra.Kind == AssociationKind.BelongsTo) {
                    Column column = FindByClassNameAndColumn(ra.OwningModel, ra.ForeignKey);
                    if (column == null)
                        continue;

                    ra.FkColumn = column;

                    // While validations on ordinary columns are associated with the columns themselves,
                    // validations on FK columns are associated with the corresponding Rails Associations
                    column.Validations = column.Validations.Concat(ra.Validations).ToArray();

                    if (ra.IsPolymorphicInterface) {
                        // In the case of a polymorphic association, its FK does not
                        // point to an actual table but to a virtual "interface",
                        // so there is nothing to set
                    } else {
                        Model referencedModel = FindByClassName(ra.OtherModel);
                        column.FkInfo = new FkInfo() {
                            ReferencedModel = referencedModel,
                        };
                    }
                }

                cleans.Add(ra);
            }

            return cleans;
        }

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
                    if (superclass.FindColumn(column.DbName) != null) {
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
                string idColumnName = _interface.Column.DbName;
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
                        association.FkSideModel = fkColumn.Owner;
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
                .Where(x => x.FkSideModel != null)
                .GroupBy(x => x.FkSideModel)
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
                            Column duplicate = table.FindColumn(column.DbName);
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
                if (_byClassName.TryGetValue(association.FkSide, out Model fkSideModel))
                    association.FkSideModel = fkSideModel;
            }
        }
        #endregion

        #region Utility Methods

        public static bool IsInteresting(Column column) {
            return UNINTERESTING_COLUMNS.Contains(column.DbName) ? false : true;
        }

        public bool UnqualifiedClassNameExists(string unqualifiedClassName) {
            if (_unqualifiedClassNames == null)
                _unqualifiedClassNames = new HashSet<string>(Models.Select(x => x.UnqualifiedClassName));

            return _unqualifiedClassNames.Contains(unqualifiedClassName);
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

        private Column FindByClassNameAndColumn(string className, string columnName) {
            Model table = FindByClassName(className);
            if (table == null)
                return null;
            Column column = table.FindColumn(columnName);
            return column;
        }

        #endregion
    }
}
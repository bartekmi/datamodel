using System;
using System.Collections.Generic;
using System.Linq;
using datamodel.utils;
using YamlDotNet.RepresentationModel;

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
                if (_schema == null)
                    _schema = ParseSchema();
                return _schema;
            }
        }
        public List<Table> Tables { get; private set; }
        public List<Association> Associations { get; private set; }
        public Dictionary<string, PolymorphicInterface> Interfaces { get; private set; }
        private Dictionary<string, Table> _byClassName;
        private HashSet<string> _teamNames;
        private HashSet<string> _unqualifiedClassNames;

        private Schema(List<Table> tables) {
            Tables = tables;
            _byClassName = tables.ToDictionary(x => x.ClassName);
            Interfaces = new Dictionary<string, PolymorphicInterface>();
        }
        #endregion

        #region Creation
        private static Schema ParseSchema() {
            YamlMappingNode root = (YamlMappingNode)YamlUtils.ReadYaml(Env.SCHEMA_FILE).RootNode;

            // Step One: Entities
            YamlSchemaParser _parser = new YamlSchemaParser();
            List<Table> tables = _parser.ParseTables(YamlUtils.GetSequence(root, "entities"));
            tables = tables.Where(x => !x.ClassName.Contains("HABTM")).ToList();        // Ignore dummy tables for "Has And Belongs To Many"

            Schema schema = new Schema(tables);

            // Step Two: Associations
            List<RailsAssociation> railsAssociations = _parser.ParseAssociations(YamlUtils.GetSequence(root, "associations"));
            railsAssociations = schema.SetFkTablesAndClean(railsAssociations);
            schema.Associations = BuildAssociations(railsAssociations)
                .Concat(BuildPolymorphicAssociations(railsAssociations))
                .ToList();

            schema.BuildPolymorphicInterfaces(railsAssociations);

            schema.Rehydrate();

            return schema;
        }

        private void BuildPolymorphicInterfaces(List<RailsAssociation> railsAssociations) {
            foreach (RailsAssociation railsAssoc in railsAssociations) {
                if (railsAssoc.IsPolymorphicInterface) {
                    PolymorphicInterface _interface = new PolymorphicInterface(railsAssoc);
                    Interfaces[_interface.Name] = _interface;
                }
            }
        }

        private static List<Association> BuildPolymorphicAssociations(List<RailsAssociation> railsAssociations) {
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

        private static List<Association> BuildAssociations(List<RailsAssociation> railsAssociations) {
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

        #region Rehydrate

        private List<RailsAssociation> SetFkTablesAndClean(List<RailsAssociation> dirties) {
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

                    if (ra.Options.Polymorphic) {
                        // In the case of a polymorphic association, its FK does not
                        // point to an actual table but to a virtual "interface",
                        // so there is nothing to set
                    } else {
                        Table referencedTable = FindByClassName(ra.OtherModel);
                        column.FkInfo = new FkInfo() {
                            ReferencedTable = referencedTable,
                        };
                    }
                }

                cleans.Add(ra);
            }

            return cleans;
        }

        private void Rehydrate() {
            ResolveSuperClasses();
            RehydrateTablesOnAssociations();
        }

        private void ResolveSuperClasses() {
            Dictionary<string, Table> byClass = Tables.ToDictionary(x => x.ClassName);

            foreach (Table table in Tables) {
                if (byClass.TryGetValue(table.SuperClassName, out Table parent)) {
                    table.Superclass = parent;
                    foreach (Column column in parent.AllColumns) {
                        Column duplicate = table.FindColumn(column.DbName);
                        if (duplicate != null)
                            table.AllColumns.Remove(duplicate);
                    }
                }
            }
        }

        private void RehydrateTablesOnAssociations() {
            foreach (Association association in Associations) {
                if (_byClassName.TryGetValue(association.OtherSide, out Table otherSideTable))
                    association.OtherSideTable = otherSideTable;
                if (_byClassName.TryGetValue(association.FkSide, out Table fkSideTable))
                    association.FkSideTable = fkSideTable;
            }
        }
        #endregion

        #region Utility Methods

        public static bool IsInteresting(Column column) {
            return UNINTERESTING_COLUMNS.Contains(column.DbName) ? false : true;
        }

        public bool TeamExists(string team) {
            if (_teamNames == null)
                _teamNames = new HashSet<string>(Tables.Select(x => x.Team));

            return _teamNames.Contains(team);
        }

        public bool UnqualifiedClassNameExists(string unqualifiedClassName) {
            if (_unqualifiedClassNames == null)
                _unqualifiedClassNames = new HashSet<string>(Tables.Select(x => x.UnqualifiedClassName));

            return _unqualifiedClassNames.Contains(unqualifiedClassName);
        }

        public Table FindByClassName(string className) {
            if (_byClassName.TryGetValue(className, out Table table))
                return table;
            return null;
        }

        public bool IsValidClassName(string className) {
            return _byClassName.ContainsKey(className);
        }

        public Table FindByDbName(string dbName) {
            return Tables.SingleOrDefault(x => x.DbName == dbName);
        }

        private Column FindByClassNameAndColumn(string className, string columnName) {
            Table table = FindByClassName(className);
            if (table == null)
                return null;
            Column column = table.FindColumn(columnName);
            return column;
        }
        #endregion
    }
}
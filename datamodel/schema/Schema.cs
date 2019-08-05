using System;
using System.Collections.Generic;
using System.Linq;
using datamodel.parser;
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
        private Dictionary<string, Table> _byClassName;
        private HashSet<string> _teamNames;

        private Schema(List<Table> tables) {
            Tables = tables;
            _byClassName = tables.ToDictionary(x => x.ClassName);
        }
        #endregion

        #region Creation
        private static Schema ParseSchema() {
            YamlMappingNode root = (YamlMappingNode)YamlUtils.ReadYaml(Env.SCHEMA_FILE).RootNode;

            // Step One: Entities
            YamlSchemaParser _parser = new YamlSchemaParser();
            List<Table> tables = _parser.ParseTables(YamlUtils.GetSequence(root, "entities"));

            Schema schema = new Schema(tables);

            // Step Two: Associations
            List<RailsAssociation> railsAssociations = _parser.ParseAssociations(YamlUtils.GetSequence(root, "associations"));
            schema.SetFkTables(railsAssociations);
            List<Association> associations = schema.BuildAssociations(railsAssociations);
            schema.Associations = associations;

            schema.Rehydrate();

            return schema;
        }

        private List<Association> BuildAssociations(List<RailsAssociation> railsAssociations) {
            Dictionary<string, RailsAssociation> dict = railsAssociations.ToDictionary(
                x => MakeKey(x, true),
                x => x
            );

            List<Association> associations = new List<Association>();
            foreach (RailsAssociation forwardAssoc in railsAssociations.Where(x => !x.IsReverse)) {
                List<RailsAssociation> ras = new List<RailsAssociation>();
                ras.Add(forwardAssoc);

                if (dict.TryGetValue(MakeKey(forwardAssoc, false), out RailsAssociation reverseAssoc))
                    ras.Add(reverseAssoc);

                associations.Add(new Association() {
                    FkSide = forwardAssoc.ActiveRecord,
                    FkSideMultiplicity = DetermineFkSideMultiplicity(reverseAssoc),
                    OtherSide = forwardAssoc.ClassName,
                    OtherSideMultiplicity = DetermineOtherSideMultiplicity(forwardAssoc, reverseAssoc),
                });
            }

            return associations;
        }

        private Multiplicity DetermineFkSideMultiplicity(RailsAssociation reverseAssociation) {
            // If there is no reverse association, by default, we assume that many entities
            // with the FK can point to the same entity
            if (reverseAssociation == null)
                return Multiplicity.Many;

            switch (reverseAssociation.Kind) {
                case AssociationKind.HasOne:
                    // I think that, in principle, this could also be ZeroOrOne, but can I tell?
                    // Can any information be garnered from the presence of 'dependent: :destroy' option?
                    return Multiplicity.One;
                case AssociationKind.HasMany:
                    return Multiplicity.Many;
                default:
                    throw new Exception("Unexpected Kind: " + reverseAssociation.Kind);
            }
        }

        private Multiplicity DetermineOtherSideMultiplicity(
            RailsAssociation forwardAssociation,
            RailsAssociation reverseAssociation
        ) {
            if (forwardAssociation.FkColumn.IsMandatory)
                if (reverseAssociation != null && reverseAssociation.Options.Destroy)
                    return Multiplicity.Aggregation;
                else
                    return Multiplicity.One;

            return Multiplicity.ZeroOrOne;
        }

        private static string MakeKey(RailsAssociation association, bool forward) {
            string source = forward ? association.ActiveRecord : association.ClassName;
            string destination = forward ? association.ClassName : association.ActiveRecord;

            return string.Format("{0}|{1}", source, destination);
        }
        #endregion


        #region Rehydrate

        private void SetFkTables(List<RailsAssociation> railsAssociations) {
            foreach (RailsAssociation ra in railsAssociations) {
                switch (ra.Kind) {
                    case AssociationKind.BelongsTo:
                        if (ra.Options.Polymorphic) {
                            throw new NotImplementedException("Polymorphic associations");
                        } else {
                            Column column = FindByClassNameAndColumn(ra.ActiveRecord, ra.ForeignKey);
                            if (column != null) {
                                Table referencedTable = FindByClassName(ra.ClassName);
                                column.FkInfo = new FkInfo() {
                                    ReferencedTable = referencedTable,
                                };
                                ra.FkColumn = column;
                            }
                        }
                        break;
                    default:
                        // Do nothing
                        break;
                }
            }
        }

        private void Rehydrate() {
            ResolveSuperClasses();
            LinkAssociations();
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

        private void LinkAssociations() {
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

        public Table FindByClassName(string className) {
            if (_byClassName.TryGetValue(className, out Table table))
                return table;
            return null;
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
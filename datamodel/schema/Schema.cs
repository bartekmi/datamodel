using System;
using System.Collections.Generic;
using System.Linq;
using datamodel.parser;
using datamodel.utils;
using YamlDotNet.RepresentationModel;

namespace datamodel.schema {
    public class Schema {

        // These columns automatically added by Rails and are of no interest
        private static readonly string[] UNINTERESTING_COLUMNS = new string[] {
            "id",
            "created_at",
            "updated_at",
            "deleted_at",
            "archived_at",
            "lock_version"
        };

        public static bool IsInteresting(Column column) {
            return UNINTERESTING_COLUMNS.Contains(column.DbName) ? false : true;
        }

        // This is a singleton
        private static Schema _schema;
        public static Schema Singleton {
            get {
                if (_schema == null)
                    _schema = ParseSchema();
                return _schema;
            }
        }

        #region Properties and Constructor
        public List<Table> Tables { get; private set; }
        public List<Association> Associations { get; private set; }
        public List<RailsAssociation> RailsAssociations { get; private set; }
        private Dictionary<string, Table> _byClassName;

        private Schema(List<Table> tables, List<Association> associations) {
            Tables = tables;
            Associations = associations;
            _byClassName = tables.ToDictionary(x => x.ClassName);
        }
        #endregion

        #region Creation
        private static Schema ParseSchema() {
            string path = "/datamodel/bartek_raw.txt";
            YamlMappingNode root = (YamlMappingNode)YamlUtils.ReadYaml(path).RootNode;

            YamlSchemaParser _parser = new YamlSchemaParser();
            List<Table> tables = _parser.ParseTables(YamlUtils.GetSequence(root, "entities"));
            List<Association> associations = _parser.ParseAssociations(YamlUtils.GetSequence(root, "relationships"));

            Schema schema = new Schema(tables, associations);
            schema.Rehydrate();
            return schema;
        }

        private void Rehydrate() {
            SetFkTables();
            ResolveSuperClasses();
            LinkAssociations();
        }

        private void SetFkTables() {
            foreach (RailsAssociation ra in Associations.SelectMany(x => x.RailsAssociations)) {
                switch (ra.Kind) {
                    case AssociationKind.BelongsTo:
                        if (ra.Options.Polymorphic) {
                            // TODO
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
                }
            }
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
                if (_byClassName.TryGetValue(association.Source, out Table sourceTable))
                    association.SourceTable = sourceTable;
                if (_byClassName.TryGetValue(association.Destination, out Table destinationTable))
                    association.DestinationTable = destinationTable;
            }
        }
        #endregion

        #region Utility Methods
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
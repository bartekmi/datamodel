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

        #region Creation
        private static Schema ParseSchema() {
            string path = "/datamodel/bartek_raw.txt";
            YamlMappingNode root = (YamlMappingNode)YamlUtils.ReadYaml(path).RootNode;

            YamlSchemaParser _parser = new YamlSchemaParser();
            List<Table> tables = _parser.ParseTables(YamlUtils.GetSequence(root, "entities"));
            List<Association> associations = _parser.ParseAssociations(YamlUtils.GetSequence(root, "relationships"));

            Schema schema = new Schema(tables, associations);
            schema.PostProcess();
            return schema;
        }

        private void PostProcess() {
            SetFkTables();
            ResolveSuperClasses();
            LinkAssociations();
        }

        private void SetFkTables() {
            // Temporarily commented out: This blows up when working with table hierarchies which are all stored in same DB table.
            // Need to re-think in light of more complex relationships than just FK.

            // foreach (FkColumn fkColumn in Tables.SelectMany(x => x.FkColumns)) {
            //     Table fkTable = FindByDbName(fkColumn.OtherTableName);
            //     if (fkTable != null)
            //         fkColumn.ReferencedTable = fkTable;
            // }
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

        public List<Table> Tables { get; private set; }
        public List<Association> Associations { get; private set; }
        private Dictionary<string, Table> _byClassName;

        private Schema(List<Table> tables, List<Association> associations) {
            Tables = tables;
            Associations = associations;
            _byClassName = tables.ToDictionary(x => x.ClassName);
        }

        public Table FindByClassName(string className) {
            if (_byClassName.TryGetValue(className, out Table table))
                return table;
            return null;
        }

        public Table FindByDbName(string dbName) {
            return Tables.SingleOrDefault(x => x.DbName == dbName);
        }
    }
}
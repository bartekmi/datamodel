using System;
using System.Collections.Generic;
using System.Linq;
using datamodel.parser;

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
                if (_schema == null) {
                    string path = "/datamodel/bartek_raw.txt";
                    YamlSchemaParser _parser = new YamlSchemaParser();
                    List<Table> tables = _parser.Parse(path, new List<Error>());
                    _schema = new Schema(tables);
                    _schema.PostProcess();
                }
                return _schema;
            }
        }

        private void PostProcess() {
            SetFkTables();
            ResolveSuperClasses();
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

        public List<Table> Tables { get; private set; }

        private Schema(List<Table> tables) {
            Tables = tables;
        }

        public Table FindByClassName(string className) {
            return Tables.FirstOrDefault(x => x.ClassName == className);
        }

        public Table FindByDbName(string dbName) {
            return Tables.SingleOrDefault(x => x.DbName == dbName);
        }
    }
}
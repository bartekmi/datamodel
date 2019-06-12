using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using datamodel.schema;
using datamodel.utils;

namespace datamodel.tools {
    public class YamlFileGenerator {

        public void Generate(Schema schema, string team) {
            foreach (Table table in schema.Tables) {
                if (table.Team != team)
                    continue;

                if (table.ModelPath == null) {
                    Error.Append(string.Format("Table {0} has no corresponding model. Class name = {1}", table.DbName, table.ClassName));
                    continue;
                }

                string filenameNoExtension = Path.GetFileNameWithoutExtension(table.ModelPath);
                string path = Path.Combine(Path.GetDirectoryName(table.ModelPath), filenameNoExtension + ".yaml");

                if (File.Exists(path))
                    continue;

                using (StreamWriter writer = new StreamWriter(path)) {
                    WriteHeader(writer, table);

                    writer.WriteLine("columns:");
                    WriteColumns(writer, table.RegularColumns);

                    writer.WriteLine("foreignKeyColumns:");
                    WriteColumns(writer, table.FkColumns);
                }
            }
        }

        private void WriteHeader(TextWriter writer, Table table) {
            writer.WriteLine(
@"description: 
group:"
            );
        }

        private void WriteColumns(TextWriter writer, IEnumerable<Column> columns) {
            foreach (Column column in columns.OrderBy(x => x.DbName))
                if (Schema.IsInteresting(column))
                    WriteColumn(writer, column);
        }

        private void WriteColumn(TextWriter writer, Column column) {
            writer.WriteLine(string.Format(
@"  - name: {0}
    description: ", column.DbName));
        }
    }
}
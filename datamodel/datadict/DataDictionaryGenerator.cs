using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema;
using datamodel.datadict.html;

namespace datamodel.datadict {
    public static class DataDictionaryGenerator {
        public static void Generate(string rootDir, IEnumerable<Table> tables) {
            foreach (Table table in tables) {
                string dir = Path.Combine(rootDir, table.Team);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filename = table.ClassName + ".html";
                string path = Path.Combine(dir, filename);

                GenerateForTable(table, path);
            }
        }

        private static void GenerateForTable(Table table, string path) {
            HtmlTag html = new HtmlTag("html");
            HtmlTag body = html.Add(new HtmlTag("body"));

            GenerateHeader(body, table);
            GenerateAttribute(body, table);
            GenerateAssociations(body, table);

            using (StreamWriter writer = new StreamWriter(path))
                html.ToHtml(writer);
        }

        private static void GenerateHeader(HtmlTag body, Table dbTable) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(dbTable.HumanName));

            if (!string.IsNullOrEmpty(dbTable.Description))
                table.AddTr(new HtmlTr(dbTable.Description));
        }

        private static void GenerateAttribute(HtmlTag body, Table dbTable) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr("Attributes"));

            foreach (Column column in dbTable.RegularColumns) {
                if (Schema.IsInteresting(column)) {
                    table.AddTr(new HtmlTr(column.HumanName).SetAttrHtml("id", column.DbName));
                    table.AddTr(new HtmlTr(column.Description));
                }
            }
        }

        private static void GenerateAssociations(HtmlTag body, Table dbTable) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr("Links / Associations"));
        }
    }
}
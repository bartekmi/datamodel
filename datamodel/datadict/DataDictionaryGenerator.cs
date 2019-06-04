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
            HtmlElement html = new HtmlElement("html");
            HtmlElement body = html.Add(new HtmlElement("body"));
            body.Add(new HtmlElement("link")
                .Attr("rel", "stylesheet")
                .Attr("href", "styles.css"));

            GenerateHeader(body, table);
            GenerateAttribute(body, table);
            GenerateAssociations(body, table);

            using (StreamWriter writer = new StreamWriter(path))
                html.ToHtml(writer);
        }

        private static void GenerateHeader(HtmlElement body, Table dbTable) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(
                 new HtmlTh(dbTable.HumanName).Attr("class", "heading1")
                ));

            if (!string.IsNullOrEmpty(dbTable.Description))
                table.Add(new HtmlTr(dbTable.Description));
        }

        private static void GenerateAttribute(HtmlElement body, Table dbTable) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(
                new HtmlTh("Attributes").Attr("class", "heading2")
                ));

            foreach (Column column in dbTable.RegularColumns) 
                if (Schema.IsInteresting(column)) {
                    table.AddTr(new HtmlTr(new HtmlTd(
                        new HtmlElement("span", column.HumanName).Attr("class", "heading3"),
                        new HtmlElement("span", "(" + column.DbTypeString + ")").Attr("class", "faded gap-left")))
                        .Attr("id", column.DbName));        // Id for anchor
                        

                    table.AddTr(new HtmlTr(column.Description)
                        .Attr("class", "text"));
                }
        }

        private static void GenerateAssociations(HtmlElement body, Table dbTable) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr("Links / Associations"));
        }
    }
}
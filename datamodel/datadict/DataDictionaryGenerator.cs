using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema;
using datamodel.datadict.html;
using datamodel.utils;
using datamodel.toplevel;

namespace datamodel.datadict {
    public static class DataDictionaryGenerator {

        public static void Generate(string rootDir, IEnumerable<Table> tables) {
            foreach (Table table in tables) {
                if (string.IsNullOrWhiteSpace(table.Team)) {
                    Console.WriteLine("Warning: Table '{0}' has no team", table.ClassName);
                    continue;
                }

                string dir = Path.Combine(rootDir, table.Team);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filename = table.SanitizedClassName + ".html";
                string path = Path.Combine(dir, filename);

                GenerateForTable(table, path);
            }
        }

        private static void GenerateForTable(Table table, string path) {
            HtmlElement html = HtmlUtils.CreatePage(out HtmlElement body);

            GenerateHeader(body, table);
            GenerateAttribute(body, table);
            GenerateAssociations(body, table);

            using (StreamWriter writer = new StreamWriter(path))
                html.ToHtml(writer, 0);
        }

        private static void GenerateHeader(HtmlElement body, Table dbTable) {
            HtmlTable table = body.Add(new HtmlTable());

            table.Add(new HtmlElement("tr",
                 new HtmlElement("th",
                    new HtmlElement("span", dbTable.HumanName).Attr("class", "heading1"),
                    new HtmlElement("span").Attr("class", "gap-left-large"),
                    HtmlUtils.MakeIconsForDiagrams(dbTable, "h1-text-icon")
                )));

            AddLabelAndData(table, "Team", dbTable.Team);
            AddLabelAndData(table, "Engine", dbTable.Engine);
            AddLabelAndData(table, "Database Table", dbTable.DbName);
            AddLabelAndData(table, "Super-Class", dbTable.SuperClassName);

            if (!string.IsNullOrEmpty(dbTable.Description))
                table.Add(new HtmlTr(dbTable.Description));
        }

        private static void AddLabelAndData(HtmlTable table, string label, string value) {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string text = string.Format("{0}: {1}", HtmlUtils.MakeBold(label), value);
            table.Add(new HtmlTr(text));
        }

        private static void GenerateAttribute(HtmlElement body, Table dbTable) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Attributes")
                .Attr("class", "heading2")));

            foreach (Column column in dbTable.RegularColumns)
                if (Schema.IsInteresting(column)) {
                    // Column Header
                    table.AddTr(new HtmlTr(
                        new HtmlTd(
                            new HtmlElement("span", column.HumanName).Attr("class", "heading3"),
                            new HtmlElement("span", "(" + column.DbTypeString + ")").Attr("class", "faded gap-left"),
                            DeprecatedSpan(column)
                        )
                      ).Attr("id", column.DbName));        // Id for anchor

                    // Column Description
                    AddDescriptionRow(table, column);
                }
        }

        private static void GenerateAssociations(HtmlElement body, Table dbTable) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Links / Associations")
                .Attr("class", "heading2")));

            foreach (Column column in dbTable.FkColumns)
                if (Schema.IsInteresting(column)) {
                    Table referencedTable = column.FkInfo.ReferencedTable;

                    HtmlBase docIcon = null;
                    HtmlBase diagramIcon = null;
                    if (referencedTable != null) {
                        docIcon = HtmlUtils.MakeIconForDocs(referencedTable);
                        diagramIcon = HtmlUtils.MakeIconsForDiagrams(referencedTable, "text-icon");
                    }

                    table.AddTr(new HtmlTr(
                        new HtmlTd(
                            new HtmlElement("span", column.HumanName).Attr("class", "heading3"),
                            new HtmlElement("span").Attr("class", "gap-left"),
                            diagramIcon,
                            new HtmlElement("span").Attr("class", "gap-left"),
                            docIcon,
                            DeprecatedSpan(column)
                        )
                    ).Attr("id", column.DbName));        // Id for anchor

                    // Column Description
                    AddDescriptionRow(table, column);
                }
        }

        private static void AddDescriptionRow(HtmlTable table, Column column) {
            HtmlTd descriptionTd = table
                .Add(new HtmlTr())
                .Add(new HtmlTd())
                .Attr("class", "text");
            foreach (string paragraphText in column.DescriptionParagraphs)
                descriptionTd.Add(new HtmlP(paragraphText));
        }

        private static HtmlElement DeprecatedSpan(IDbElement dbElement) {
            if (dbElement.Deprecated)
                return new HtmlElement("span", "[DEPRECATED]").Attr("class", "heading3 gap-left");
            return null;
        }
    }
}
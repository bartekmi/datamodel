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

        public static void Generate(string rootDir, IEnumerable<Model> tables) {
            foreach (Model table in tables) {
                if (string.IsNullOrWhiteSpace(table.Team)) {
                    Console.WriteLine("Warning: Model '{0}' has no team", table.ClassName);
                    continue;
                }

                string dir = Path.Combine(rootDir, table.Team);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filename = table.SanitizedClassName + ".html";
                string path = Path.Combine(dir, filename);

                GenerateForModel(table, path);
            }
        }

        private static void GenerateForModel(Model table, string path) {
            HtmlElement html = HtmlUtils.CreatePage(out HtmlElement body);

            GenerateHeader(body, table);
            GenerateAttribute(body, table);
            GenerateAssociations(body, table);

            using (StreamWriter writer = new StreamWriter(path))
                html.ToHtml(writer, 0);
        }

        private static void GenerateHeader(HtmlElement body, Model dbModel) {
            HtmlTable table = body.Add(new HtmlTable());

            table.Add(new HtmlElement("tr",
                 new HtmlElement("th",
                    new HtmlElement("span", dbModel.HumanName).Attr("class", "heading1"),
                    new HtmlElement("span").Attr("class", "gap-left-large"),
                    HtmlUtils.MakeIconsForDiagrams(dbModel, "h1-text-icon")
                )));

            AddLabelAndData(table, "Team", dbModel.Team);
            AddLabelAndData(table, "Engine", dbModel.Engine);
            AddLabelAndData(table, "Database Table", dbModel.DbName);
            AddLabelAndData(table, "Super-Class", dbModel.SuperClassName);

            if (!string.IsNullOrEmpty(dbModel.Description))
                table.Add(new HtmlTr(dbModel.Description));
        }

        private static void AddLabelAndData(HtmlTable table, string label, string value) {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string text = string.Format("{0}: {1}", HtmlUtils.MakeBold(label), value);
            table.Add(new HtmlTr(text));
        }

        private static void GenerateAttribute(HtmlElement body, Model dbModel) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Attributes")
                .Attr("class", "heading2")));

            foreach (Column column in dbModel.RegularColumns)
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

        private static void GenerateAssociations(HtmlElement body, Model dbModel) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Links / Associations")
                .Attr("class", "heading2")));

            foreach (Column column in dbModel.FkColumns)
                if (Schema.IsInteresting(column)) {
                    Model referencedModel = column.FkInfo.ReferencedModel;

                    HtmlBase docIcon = null;
                    HtmlBase diagramIcon = null;
                    if (referencedModel != null) {
                        docIcon = HtmlUtils.MakeIconForDocs(referencedModel);
                        diagramIcon = HtmlUtils.MakeIconsForDiagrams(referencedModel, "text-icon");
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
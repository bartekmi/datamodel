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

        public static void Generate(string rootDir, IEnumerable<Model> models) {
            foreach (Model model in models) {
                string dir = model.Level1 == null ? rootDir : Path.Combine(rootDir, model.Level1);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filename = model.SanitizedClassName + ".html";
                string path = Path.Combine(dir, filename);

                GenerateForModel(model, path);
            }
        }

        private static void GenerateForModel(Model model, string path) {
            HtmlElement html = HtmlUtils.CreatePage(out HtmlElement body);

            GenerateModelHeader(body, model);
            GenerateModelAttributes(body, model);
            GenerateModelOutgoingAssociations(body, model);
            GenerateModelIncomingAssociations(body, model);

            using (StreamWriter writer = new StreamWriter(path))
                html.ToHtml(writer, 0);
        }

        private static void GenerateModelHeader(HtmlElement body, Model model) {
            Schema schema = Schema.Singleton;
            HtmlTable table = body.Add(new HtmlTable());

            table.Add(new HtmlElement("tr",
                 new HtmlElement("th",
                    new HtmlElement("span", model.HumanName).Class("heading1"),
                    new HtmlElement("span").Class("gap-left-large"),
                    HtmlUtils.MakeIconsForDiagrams(model, "h1-text-icon")
                )));

            AddLabelAndData(table, schema.Level1, model.Level1);
            AddLabelAndData(table, schema.Level2, model.Level2);
            AddLabelAndData(table, schema.Level3, model.Level3);

            AddLabelAndData(table, "Name", model.Name);
            if (model.Superclass != null)
                AddLabelAndData(table, "Super-Class", model.SuperClassName);


            if (!string.IsNullOrEmpty(model.Description))
                table.Add(new HtmlTr(model.Description));
        }

        private static void AddLabelAndData(HtmlTable table, string label, string value, bool isLink = false) {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string text = string.Format("{0}: {1}", HtmlUtils.MakeBold(label),
                isLink ? HtmlUtils.MakeLink(value, value).ToString() : value);
            table.Add(new HtmlTr(text));
        }

        private static void GenerateModelAttributes(HtmlElement body, Model dbModel) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Attributes").Class("heading2")));

            foreach (Column column in dbModel.RegularColumns)
                if (Schema.Singleton.IsInteresting(column)) {
                    // Column Header
                    table.AddTr(new HtmlTr(
                        new HtmlTd(
                            new HtmlElement("span", column.HumanName).Class("heading3"),
                            new HtmlElement("span", "(" + column.DataType + ")").Class("faded gap-left"),
                            DeprecatedSpan(column)
                        )
                      ).Attr("id", column.Name)
                       .Class("attribute"));        // Id for anchor

                    // Column Description
                    AddDescriptionRow(table, column);

                    // Column Enum Values
                    AddEnumValuesRow(table, column);
                }
        }

        private static void GenerateModelOutgoingAssociations(HtmlElement body, Model dbModel) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Outgoing Foreign Key Links / Associations").Class("heading2")));

            foreach (Column column in dbModel.FkColumns)
                AddFkColumnInfo(table, column, column.FkInfo.ReferencedModel, x => x.HumanName);
        }

        private static void GenerateModelIncomingAssociations(HtmlElement body, Model dbModel) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Incoming Foreign Key Links / Associations").Class("heading2")));

            foreach (Column column in Schema.Singleton.IncomingFkColumns(dbModel))
                AddFkColumnInfo(table, column, column.Owner, x => string.Format("{0}.{1}", x.Owner.HumanName, x.HumanName));
        }

        private static void AddFkColumnInfo(HtmlTable table, Column column, Model other, Func<Column, string> nameFunc) {
            if (Schema.Singleton.IsInteresting(column)) {
                HtmlBase docIcon = other == null ? null : HtmlUtils.MakeIconForDocs(other);
                HtmlBase diagramIcon = other == null ? null : HtmlUtils.MakeIconsForDiagrams(other, "text-icon");

                // Column Header
                table.AddTr(new HtmlTr(
                    new HtmlTd(
                        new HtmlElement("span", nameFunc(column)).Class("heading3"),
                        new HtmlElement("span").Class("gap-left"),
                        diagramIcon,
                        new HtmlElement("span").Class("gap-left"),
                        docIcon,
                        DeprecatedSpan(column)
                    )
                ).Attr("id", column.Name));        // Id for anchor

                // Column Description
                AddDescriptionRow(table, column);
            }
        }

        private static void AddDescriptionRow(HtmlTable table, Column column) {
            HtmlTd descriptionTd = table
                .Add(new HtmlTr())
                .Add(new HtmlTd())
                .Class("text");
            foreach (string paragraphText in column.DescriptionParagraphs)
                descriptionTd.Add(new HtmlP(paragraphText));
        }

        private static void AddEnumValuesRow(HtmlTable table, Column column) {
            if (column.Enum != null) {
                HtmlTd enumValuesTd = table
                    .Add(new HtmlTr())
                    .Add(new HtmlTd());

                enumValuesTd.Add(new HtmlTable(
                        new HtmlTr(new HtmlTh("Enum Values"),
                            new HtmlTh("Enum Descriptions")).Class("enum-header"),
                        column.Enum.Values.Select(x =>
                            new HtmlTr(x.Key.ToString(), x.Value).Class("enum-data"))
                    ).Class("enum-table")
                );
            }
        }

        private static HtmlElement DeprecatedSpan(IDbElement dbElement) {
            if (dbElement.Deprecated)
                return new HtmlElement("span", "[DEPRECATED]").Class("heading3 gap-left");
            return null;
        }
    }
}
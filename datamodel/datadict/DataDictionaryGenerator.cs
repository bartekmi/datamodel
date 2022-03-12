using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema;
using datamodel.datadict.html;
using datamodel.toplevel;

namespace datamodel.datadict {
    public static class DataDictionaryGenerator {

        public static void Generate(string rootDir, IEnumerable<Model> models) {
            foreach (Model model in models) {
                string dir = model.Level1 == null ? rootDir : Path.Combine(rootDir, model.Level1);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filename = model.SanitizedQualifiedName + ".html";
                string path = Path.Combine(dir, filename);

                GenerateForModel(model, path);
            }
        }

        private static void GenerateForModel(Model model, string path) {
            HtmlElement html = HtmlUtils.CreatePage(out HtmlElement body);

            GenerateModelHeader(body, model);
            GenerateModelDerivedClasses(body, model);
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
                    new HtmlElement("span", HtmlUtils.MakeLink("/", "Index").Text, true).Class("heading1"),
                    new HtmlElement("span").Class("gap-left-large"),
                    new HtmlElement("span", "|").Class("heading1"),
                    new HtmlElement("span").Class("gap-left-large"),
                    new HtmlElement("span", model.HumanName).Class("heading1"),
                    new HtmlElement("span").Class("gap-left-large"),
                    HtmlUtils.MakeIconsForDiagrams(model, "h1-text-icon")
                )));

            AddLabelAndData(table, schema.Level1, model.Level1);
            AddLabelAndData(table, schema.Level2, model.Level2);
            AddLabelAndData(table, schema.Level3, model.Level3);

            AddLabelAndData(table, "Name", model.Name);
            AddLabelAndData(table, "Qualified Name", model.QualifiedName);
            AddLabelAndData(table, "Super-Class", model.SuperClassName);

            foreach (Label label in model.Labels)
                AddLabelAndData(table, label.Name, label.Value);


            if (!string.IsNullOrEmpty(model.Description))
                table.Add(new HtmlTr(model.Description));
        }

        private static void AddLabelAndData(HtmlTable table, string label, string value, bool isLink = false) {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string text = string.Format("{0}: {1}", HtmlUtils.MakeBold(label),
                isLink ? HtmlUtils.MakeLink(value, value).ToString() : value);
            table.Add(new HtmlTr(text, true));
        }

        private static void GenerateModelDerivedClasses(HtmlElement body, Model dbModel) {
            if (dbModel.DerivedClasses.Count == 0)
                return;

            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Derived Classes").Class("heading2")));

            foreach (Model derived in dbModel.DerivedClasses.OrderBy(x => x.HumanName)) {
                string link = HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(derived), derived.HumanName).Text;

                HtmlBase diagramIcon = HtmlUtils.MakeIconsForDiagrams(derived, "text-icon");

                // Header
                HtmlTr tr = new HtmlTr(
                    new HtmlTd(
                        new HtmlElement("span", link, true).Class("heading3"),
                        new HtmlElement("span").Class("gap-left"),
                        diagramIcon,
                        new HtmlElement("span").Class("gap-left"),
                        DeprecatedSpan(derived)
                    )
                );

                table.AddTr(tr);

                // Description
                AddDescriptionRow(table, derived);
            }
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

            table.AddTr(new HtmlTr(new HtmlTh("Outgoing Associations").Class("heading2")));

            foreach (Column column in dbModel.RefColumns) {
                Model referenced = column.ReferencedModel;
                string link = HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(referenced), referenced.HumanName).Text;
                string name = string.Format("{0} ({1})", column.HumanName, link);

                AddRefColumnInfo(table, column, column.ReferencedModel, name);
            }
        }

        private static void GenerateModelIncomingAssociations(HtmlElement body, Model dbModel) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Incoming Associations").Class("heading2")));
            var orderedIncoming = Schema.Singleton.IncomingRefColumns(dbModel)
                .OrderBy(x => x.Owner.HumanName)
                .ThenBy(x => x.HumanName);

            foreach (Column column in orderedIncoming) {
                Model referenced = column.Owner;
                string link = HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(referenced), referenced.HumanName).Text;
                string name = string.Format("{0}.{1}", link, column.HumanName);

                AddRefColumnInfo(table, column, column.Owner, name); 
            }
        }

        private static void AddRefColumnInfo(HtmlTable table, Column column, Model other, string name) {
            if (Schema.Singleton.IsInteresting(column)) {
                HtmlBase diagramIcon = other == null ? null : HtmlUtils.MakeIconsForDiagrams(other, "text-icon");

                // Column Header
                HtmlTr tr = new HtmlTr(
                    new HtmlTd(
                        new HtmlElement("span", name, true).Class("heading3"),
                        new HtmlElement("span").Class("gap-left"),
                        diagramIcon,
                        new HtmlElement("span").Class("gap-left"),
                        DeprecatedSpan(column)
                    )
                ).Attr("id", column.Name);        // Id for anchor

                table.AddTr(tr);

                // Column Description
                AddDescriptionRow(table, column);
            }
        }

        private static void AddDescriptionRow(HtmlTable table, IDbElement element) {
            HtmlTd descriptionTd = table
                .Add(new HtmlTr())
                .Add(new HtmlTd())
                .Class("text");

             foreach (string paragraphText in element.DescriptionParagraphs())
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
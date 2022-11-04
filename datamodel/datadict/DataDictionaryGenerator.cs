using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using datamodel.schema;
using datamodel.datadict.html;
using datamodel.toplevel;
using datamodel.utils;

namespace datamodel.datadict {
    public static class DataDictionaryGenerator {

        public static void Generate(string rootDir, IEnumerable<Model> models) {
            foreach (Model model in models) {
                string dir = model.GetLevel(0) == null ? rootDir : Path.Combine(rootDir, model.GetLevel(0));
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
            GenerateModelMethods(body, model);
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
                    HtmlUtils.MakeIconsForDiagrams(model, "h1-text-icon"),
                    DeprecatedSpan(model)
                )));

            for (int ii = 0; ii < model.Levels.Length; ii++)
                AddLabelAndData(table, schema.GetLevelName(ii), model.GetLevel(ii));

            AddLabelAndData(table, "Name", model.Name);
            AddLabelAndData(table, "Qualified Name", model.QualifiedName);
            AddLabelAndData(table, "Super-Class", model.SuperClassName, UrlService.Singleton.DocUrl(model.Superclass));

            foreach (Label label in model.Labels)
                AddLabelAndData(table, label.Name, label.Value, label.IsUrl ? label.Value : null);


            if (!string.IsNullOrEmpty(model.Description))
                table.Add(new HtmlTr(model.Description));
        }

        private static void AddLabelAndData(HtmlTable table, string label, string value, string url = null) {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string text = string.Format("{0}: {1}", HtmlUtils.MakeBold(label),
                url == null ? value : HtmlUtils.MakeLink(url, value).ToString());
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

            foreach (Property property in dbModel.RegularProperties)
                if (Schema.Singleton.IsInteresting(property)) {
                    // Property Header
                    table.AddTr(new HtmlTr(
                        new HtmlTd(
                            property.CanBeEmpty ? null : HtmlUtils.MakeIcon(IconUtils.CHECKMARK, null, "This attribute must be specified"),
                            new HtmlElement("span", property.HumanName).Class("heading3"),
                            new HtmlElement("span", "(" + property.DataType + ")").Class("faded gap-left"),
                            DeprecatedSpan(property)
                        )
                      ).Attr("id", property.Name)
                       .Class("attribute"));        // Id for anchor

                    // Property Description
                    AddDescriptionRow(table, property);

                    // Optional labels
                    foreach (Label label in property.Labels)
                        AddLabelAndData(table, label.Name, label.Value, label.IsUrl ? label.Value : null);

                    // Property Enum Values
                    AddEnumValuesRow(table, property);
                }
        }

        private static void GenerateModelMethods(HtmlElement body, Model model) {
            if (model.Methods.Count == 0)
                return;

            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Methods").Class("heading2")));

            foreach (Method method in model.Methods) {
                // Method Header
                table.AddTr(new HtmlTr(
                    new HtmlTd(
                        new HtmlElement("span", method.HumanRepresentation(TypeToHtml), true).Class("heading3"),
                        DeprecatedSpan(method)
                    )
                    ).Attr("id", method.Name)
                    .Class("attribute"));        // Id for anchor

                // Method Description
                AddDescriptionRow(table, method);

                // Optional labels
                foreach (Label label in method.Labels)
                    AddLabelAndData(table, label.Name, label.Value, label.IsUrl ? label.Value : null);
            }
        }

        private static string TypeToHtml(NamedType type) {
            StringBuilder builder = new StringBuilder();

            Model referenced = type.Type.ReferencedModel;
            if (referenced != null) {
                string link = HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(referenced), referenced.HumanName).Text;
                builder.Append(link);
            } else if (type.Type.Enum != null)
                builder.Append(type.Type.Enum.Name);
            else
                builder.Append(type.Type.Name);

            if (!string.IsNullOrWhiteSpace(type.Name)) {
                builder.Append(" ");
                builder.Append(type.Name);
            }
            return builder.ToString();
        }

        private static void GenerateModelOutgoingAssociations(HtmlElement body, Model dbModel) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Outgoing Associations").Class("heading2")));

            foreach (Property property in dbModel.RefProperties) {
                Model referenced = property.ReferencedModel;
                string link = HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(referenced), referenced.HumanName).Text;
                string name = string.Format("{0} ({1})", property.HumanName, link);
                bool isRequired = !property.CanBeEmpty;

                AddRefPropertyInfo(table, property, property.ReferencedModel, name, isRequired);
            }
        }

        private static void GenerateModelIncomingAssociations(HtmlElement body, Model dbModel) {
            HtmlTable table = body.Add(new HtmlTable());

            table.AddTr(new HtmlTr(new HtmlTh("Incoming Associations").Class("heading2")));
            var orderedIncoming = Schema.Singleton.IncomingRefProperties(dbModel)
                .OrderBy(x => x.Owner.HumanName)
                .ThenBy(x => x.HumanName);

            foreach (Property property in orderedIncoming) {
                Model referenced = property.Owner;
                string link = HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(referenced), referenced.HumanName).Text;
                string name = string.Format("{0}.{1}", link, property.HumanName);

                AddRefPropertyInfo(table, property, property.Owner, name, false);
            }
        }

        private static void AddRefPropertyInfo(HtmlTable table, Property property, Model other, string name, bool isRequired) {
            if (Schema.Singleton.IsInteresting(property)) {
                HtmlBase diagramIcon = other == null ? null : HtmlUtils.MakeIconsForDiagrams(other, "text-icon");

                // Property Header
                HtmlTr tr = new HtmlTr(
                    new HtmlTd(
                        isRequired ? HtmlUtils.MakeIcon(IconUtils.CHECKMARK, null, "This associated object must be specified") : null,
                        new HtmlElement("span", name, true).Class("heading3"),
                        new HtmlElement("span").Class("gap-left"),
                        diagramIcon,
                        new HtmlElement("span").Class("gap-left"),
                        DeprecatedSpan(property)
                    )
                ).Attr("id", property.Name);        // Id for anchor

                table.AddTr(tr);

                // Property Description
                AddDescriptionRow(table, property);
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

        private static void AddEnumValuesRow(HtmlTable table, Property property) {
            if (property.Enum != null) {
                HtmlTd enumValuesTd = table
                    .Add(new HtmlTr())
                    .Add(new HtmlTd());

                enumValuesTd.Add(new HtmlTable(
                        new HtmlTr(
                            new HtmlTh("Enum Values"),
                            new HtmlTh("Enum Descriptions"))
                            .Class("enum-header"),
                        property.Enum.Values.Select(x =>
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
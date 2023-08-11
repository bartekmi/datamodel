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
                string dir = Path.Combine(rootDir, model.RootLevel ?? UrlService.DATA_DICT_SUBDIR);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filename = model.SanitizedQualifiedName + ".html";
                string path = Path.Combine(dir, filename);

                GenerateForModel(model, path);
            }
        }

        private static void GenerateForModel(Model model, string path) {
            HtmlElement html = HtmlUtils.CreatePage(out HtmlElement body, true);

            GenerateModelHeader(body, model);
            GenerateModelDerivedClasses(body, model);
            GenerateModelAttributes(body, model);
            GenerateModelMethods(body, model);
            GenerateModelOutgoingAssociations(body, model);
            GenerateModelIncomingAssociations(body, model);

            using StreamWriter writer = new(path);
            html.ToHtml(writer, 0);
        }

        private static void GenerateModelHeader(HtmlElement body, Model model) {
            Schema schema = Schema.Singleton;
            HtmlTable table = body.Add(new HtmlTable());

            table.Add(new HtmlElement("tr",
                 new HtmlElement("th",
                    new HtmlElement("span", HtmlUtils.MakeLink("..", "Index").Text, true).Class("heading1"),
                    new HtmlElement("span").Class("gap-left-large"),
                    new HtmlElement("span", "|").Class("heading1"),
                    new HtmlElement("span").Class("gap-left-large"),
                    new HtmlElement("span", model.HumanName).Class("heading1"),
                    new HtmlElement("span").Class("gap-left-large"),
                    HtmlUtils.MakeIconsForDiagrams(model, "h1-text-icon", true),
                    DeprecatedSpan(model)
                )));

            for (int ii = 0; ii < model.Levels.Length; ii++)
                AddLabelAndData(table, schema.GetLevelName(ii), model.GetLevel(ii));

            AddLabelAndData(table, "Name", model.Name);
            AddLabelAndData(table, "Qualified Name", model.QualifiedName);
            AddLabelAndData(table, "Super-Class", model.SuperClassName, UrlService.Singleton.DocUrl(model.Superclass, true));

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
                string link = HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(derived, true), derived.HumanName).Text;

                HtmlBase diagramIcon = HtmlUtils.MakeIconsForDiagrams(derived, "text-icon", true);

                // Header
                HtmlTr tr = new(
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
                            property.CanBeEmpty ? 
                                HtmlUtils.MakeIcon(IconUtils.ON_OFF, null, "This attribute is OPTIONAL", true) : 
                                HtmlUtils.MakeIcon(IconUtils.CHECKMARK, null, "This attribute is REQUIRED", true),

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

                    AddSeparator(table);
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
            StringBuilder builder = new();

            Model referenced = type.Type.ReferencedModel;
            if (referenced != null) {
                string link = HtmlUtils.MakeLink(
                    UrlService.Singleton.DocUrl(referenced, true), 
                    referenced.HumanName).Text;
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

            foreach (Association assoc in Schema.Singleton.Associations.Where(x => x.OwnerSideModel == dbModel)) {
                Model referenced = assoc.OtherSideModel;
                Property property = assoc.RefProperty;
                string link = HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(referenced, true), 
                    referenced.HumanName).Text;
                string mainHtml = string.Format("{0} ({1})", property.HumanName, link);
                bool isRequired = !property.CanBeEmpty;

                AddRefPropertyInfo(table, property, referenced, mainHtml, assoc.OtherMultiplicity);
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
                string link = HtmlUtils.MakeLink(
                    UrlService.Singleton.DocUrl(referenced, true), 
                    referenced.HumanName).Text;
                string mainHtml = string.Format("{0}.{1}", link, property.HumanName);

                AddRefPropertyInfo(table, property, property.Owner, mainHtml, null);
            }
        }

        private static void AddRefPropertyInfo(HtmlTable table, Property property, Model other, string mainHtml, Multiplicity? multiplicity) {
            if (Schema.Singleton.IsInteresting(property)) {
                HtmlBase diagramIcon = other == null ? null : HtmlUtils.MakeIconsForDiagrams(other, "text-icon", true);

                string multiplicityIcon = null;
                string multiplicityToolTip = null;
                switch (multiplicity) {
                    case Multiplicity.One:
                        multiplicityIcon = IconUtils.CHECKMARK;
                        multiplicityToolTip = "This associated object is REQUIRED";
                        break;
                    case Multiplicity.ZeroOrOne:
                        multiplicityIcon = IconUtils.ON_OFF;
                        multiplicityToolTip = "This associated object is OPTIONAL";
                        break;
                    case Multiplicity.Many:
                        multiplicityIcon = IconUtils.ONE_TO_MANY;
                        multiplicityToolTip = "One-to-many association";
                        break;
                }

                // Property Header
                HtmlTr tr = new HtmlTr(
                    new HtmlTd(
                        multiplicityIcon == null ? null : HtmlUtils.MakeIcon(multiplicityIcon, null, multiplicityToolTip, true),
                        new HtmlElement("span", mainHtml, true).Class("heading3"),
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
            if (string.IsNullOrWhiteSpace(element.Description))
                return;

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

        private static void AddSeparator(HtmlTable table) {
            table
                .Add(new HtmlTr())
                .Add(new HtmlTd())
                .Class("text");
        }

        private static HtmlElement DeprecatedSpan(IDbElement dbElement) {
            if (dbElement.Deprecated)
                return new HtmlElement("span", "[DEPRECATED]").Class("heading3 gap-left");
            return null;
        }
    }
}
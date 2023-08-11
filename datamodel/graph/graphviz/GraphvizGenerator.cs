using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema;
using datamodel.graphviz.dot;
using datamodel.utils;
using datamodel.toplevel;
using datamodel.metadata;

namespace datamodel.graphviz {

    public class GraphvizGenerator {

        #region Top-Level
        // E.g. see https://github.com/jrfonseca/gprof2dot/issues/30
        // Graphviz imposes a maximum string length of 16384. Giving a bit of breathing room.
        const int MAX_ENUM_ITEMS = 30;
        const int MAX_TOOLTIP_LENGTH = 5000;

        public void GenerateGraph(
            GraphDefinition graphDef,
            IEnumerable<Model> models,
            IEnumerable<Association> associations,
            IEnumerable<Model> extraModels,
            List<PolymorphicInterface> interfaces) {

            Graph graph = CreateGraph(models, associations, extraModels, interfaces)
                .SetAttrGraph("pad", "0.5")
                .SetAttrGraph("nodesep", "1")
                .SetAttrGraph("ranksep", "1")
                .SetAttrGraph("notranslate", true);

            if (graphDef.Sep != null)
                graph.SetAttrGraph("sep", "+" + graphDef.Sep.Value);

            if (graphDef.Len != null)
                graph.SetAttrGraph("len", graphDef.Len.Value);

            string baseName = graphDef.FullyQualifiedName;
            GraphvizRunner.CreateDotAndRun(graph, baseName, graphDef.Style);
        }

        public Graph CreateGraph(IEnumerable<Model> models, IEnumerable<Association> associations, IEnumerable<Model> extraModels, List<PolymorphicInterface> interfaces) {
            Graph graph = new();
            // Graphviz forces the images to be available on disk, even though they are not needed for SVG
            // This means that the build path to the images has to be the same as the web deploy path, which is annoying
            // I've left the following line commented out in case this ever actually works as it should.
            //.SetAttrGraph("imagepath", IMAGE_PATH);

            IEnumerable<Model> allModels = models.Union(extraModels);

            foreach (Model model in models.Where(x => !x.HasPolymorphicInterfaces))
                graph.AddNode(ModelToNode(allModels, model));

            foreach (Model model in models.Where(x => x.HasPolymorphicInterfaces))
                graph.AddSubgraph(PolymorphicModelToSubgraph(allModels, model));

            foreach (Model model in extraModels)
                graph.AddNode(ExtraModelToNode(null, model));

            foreach (Association association in associations)
                graph.AddEdge(AssociationToEdge(association));

            foreach (Model model in allModels.Where(x => x.Superclass != null))
                if (allModels.Contains(model.Superclass))
                    graph.AddEdge(SuplerclassLinkToEdge(model));

            return graph;
        }
        #endregion

        #region Superclass Relationships
        private Edge SuplerclassLinkToEdge(Model model) {
            // The source/destination ordering here is important and forces the 
            // superclass to be above the derived class
            Edge edge = new() {
                Source = ModelToNodeId(model.Superclass),
                Destination = ModelToNodeId(model),
            };

            edge.SetAttrGraph("dir", "both")        // Allows for both ends of line to be decorated
                .SetAttrGraph("arrowsize", 1.5)     // I wanted to make this larger but the arrow icons overlap
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("arrowhead", "none")          // Straight connection - no decoration
                .SetAttrGraph("arrowtail", "onormal")       // "normal" would be an arrow. "onormal" makes the arrow empty.
                .SetAttrGraph("tailport", "s");     // Forces arrow to connect to center bottom. 

            return edge;
        }
        #endregion

        #region Polymorphic Associations

        private Subgraph PolymorphicModelToSubgraph(IEnumerable<Model> allModels, Model model) {
            Subgraph container = new Subgraph() {
                Name = "cluster_" + ModelToNodeId(model),
            }.SetAttrGraph("pencolor", "white");

            container.AddNode(ModelToNode(allModels, model));

            foreach (PolymorphicInterface _interface in model.PolymorphicInterfaces) {
                container.AddNode(PolymorphicInterfaceToNode(_interface));
                container.AddEdge(PolymorphicInterfaceToEdge(_interface));
            }

            return container;
        }

        private Node PolymorphicInterfaceToNode(PolymorphicInterface _interface) {
            Node node = new() {
                Name = _interface.Name,
            };

            IEnumerable<string> links = Schema.Singleton.PolymorphicAssociationsForInterface(_interface)
                .Select(x => string.Format("{0} {1}",
                    HtmlUtils.BULLET,
                    x.PolymorphicReverseName));

            string tooltip = string.Format("Polymorphic Association: {0}{1}{1}Used By:{1}{2}",
                _interface.Name,
                HtmlUtils.LINE_BREAK,
                string.Join(HtmlUtils.LINE_BREAK, links));

            node.SetAttrGraph("tooltip", tooltip)
                .SetAttrGraph("shape", "circle")
                .SetAttrGraph("label", "PMA")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("margin", 0);

            return node;
        }

        private Edge PolymorphicInterfaceToEdge(PolymorphicInterface _interface) {
            Edge edge = new() {
                Source = ModelToNodeId(_interface.Model),
                Destination = _interface.Name,
            };

            edge.SetAttrGraph("arrowhead", "none");

            return edge;
        }
        #endregion

        #region Models
        private Node ModelToNode(IEnumerable<Model> allModels, Model model) {
            Node node = new() {
                Name = ModelToNodeId(model),
            };

            node.SetAttrGraph("style", "filled")
                .SetAttrGraph("fillcolor", model.ColorString)
                .SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("label", CreateLabel(allModels, model, true));

            return node;
        }

        #endregion

        #region Extra Models - Not part of graph but added as "glue"
        private Node ExtraModelToNode(IEnumerable<Model> models, Model model) {
            Node node = new() {
                Name = ModelToNodeId(model),
            };

            node.SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("height", 1.0)                // Minimum height in inches... Allows for more connections
                .SetAttrGraph("label", CreateLabel(models, model, false));

            return node;
        }
        #endregion

        #region Common Node Creation - for both Models and Extra Models

        private HtmlEntity CreateLabel(IEnumerable<Model> models, Model dbModel, bool includeProperties) {

            HtmlTable table = new HtmlTable()
                .SetAttrHtml("border", 0)
                .SetAttrHtml("cellborder", 0)
                .SetAttrHtml("cellspacing", 0);

            // Header
            string headerText = HtmlUtils.SetFont(HtmlUtils.MakeBold(dbModel.HumanName), 16);

            HtmlTd headerTd = new HtmlTd(headerText)
                .SetAttrHtml("tooltip", CreateModelToolTip(dbModel))
                .SetAttrHtml("href", UrlService.Singleton.DocUrl(dbModel, false));

            table.AddTr(new HtmlTr(headerTd));

            if (!includeProperties)
                return table;

            CreateLabelProperties(models, dbModel, table);
            CreateLabelMethods(dbModel, table);

            return table;
        }

        private void CreateLabelMethods(Model dbModel, HtmlTable table) {
        foreach (Method method in dbModel.Methods) {
                HtmlTr row = new();

                HtmlTd propertyNameTd = new() {
                    Text = method.HumanShortRepresentation,
                };
                row.AddTd(propertyNameTd);

                // Attributes for the property name Html-like element
                propertyNameTd
                    .SetAttrHtml("align", "left")
                    .SetAttrHtml("tooltip", CreateMemberToolTip(method, null))
                    .SetAttrHtml("href", method.DocUrl);

                table.AddTr(row);
            }
        }

        private void CreateLabelProperties(IEnumerable<Model> models, Model model, HtmlTable table) {
            HashSet<datamodel.schema.Enum> usedEnums = new();

            foreach (Property property in model.AllProperties) {
                if (Schema.Singleton.IsInteresting(property) &&
                    !property.Deprecated &&
                    !(property.IsPolymorphicId || property.IsPolymorphicType)) {

                    HtmlTr row = new();
                    Model referencedModel = property.IsRef ? property.ReferencedModel : null;

                    string propertyName = HtmlUtils.BULLET + property.HumanName;
                    HtmlTd propertyNameTd = new();
                    row.AddTd(propertyNameTd);

                    // Mandatory?
                    if (!property.CanBeEmpty) {
                        row.AddTd(new HtmlTd(HtmlUtils.MakeImage(IconUtils.CHECKMARK_SMALL, false))
                            .SetAttrHtml("tooltip", property.IsRef ?
                                "This associated object must be specified" :
                                "This attribute must be specified")
                            .SetAttrHtml("href", property.DocUrl));
                    }

                    // Ref Property
                    if (property.IsRef) {
                        if (models.Contains(referencedModel))
                            continue;           // Do not include Ref property if in this graph... It will be shown via an association line

                        propertyNameTd.Text = propertyName;

                        if (referencedModel != null) {
                            // TODO(bartekmi) Currently, we only show a link to the largest graph. Consider exposing links to all sizes.
                            GraphDefinition graphDef = UrlService.Singleton.GetReasonableGraph(referencedModel);
                            if (graphDef != null) {
                                string graphToolTip = string.Format("Go to diagram which contains the Model '{0}'\n\nTitle: {1}\n\nNumber of Models: {2}",
                                    referencedModel.HumanName,
                                    graphDef.HumanName,
                                    graphDef.CoreModels.Length);

                                row.AddTd(new HtmlTd(HtmlUtils.MakeImage(IconUtils.DIAGRAM_SMALL, false))
                                   .SetAttrHtml("tooltip", graphToolTip)
                                   .SetAttrHtml("href", graphDef.GetSvgUrl(true)));
                            }

                            row.AddTd(new HtmlTd(HtmlUtils.MakeImage(IconUtils.DOCS_SMALL, false))
                               .SetAttrHtml("tooltip", string.Format("Click to go to Data Dictionary of linked model: '{0}'", referencedModel.HumanName))
                               .SetAttrHtml("href", UrlService.Singleton.DocUrl(referencedModel, false)));

                            row.SetAttrAllChildren("bgcolor", referencedModel.ColorString);
                        }
                    }
                    // Regular Property
                    else {
                        string dataType = string.Format("<FONT COLOR=\"gray25\">({0})</FONT>", ToShortType(property));
                        propertyNameTd.Text = string.Format("{0} {1}", propertyName, dataType);
                    }

                    // Attributes for the property name Html-like element
                    datamodel.schema.Enum theEnum = property.Enum;
                    propertyNameTd
                        .SetAttrHtml("align", "left")
                        .SetAttrHtml("tooltip", CreateMemberToolTip(property, 
                            usedEnums.Contains(theEnum) ? null : theEnum))
                        .SetAttrHtml("href", property.DocUrl);

                    usedEnums.Add(theEnum);

                    table.AddTr(row);
                }
            }
        }

        private static string CreateMemberToolTip(IDbElement property, datamodel.schema.Enum theEnum) {
            StringBuilder builder = new();

            if (!string.IsNullOrEmpty(property.Description)) 
                builder.AppendLine(property.DescriptionParagraphs().First());

            if (theEnum != null) {
                builder.AppendLine(HtmlUtils.LINE_BREAK);
                foreach (var value in theEnum.Values.Take(MAX_ENUM_ITEMS)) {
                    builder.AppendLine(string.Format("{0}{1}: {2}",
                        HtmlUtils.LINE_BREAK,
                        value.Key,
                        value.Value));
                }
                
                if (theEnum.Values.Count() > MAX_ENUM_ITEMS)
                    builder.AppendLine(HtmlUtils.LINE_BREAK + "... (Truncated)");
            }

            if (property.Labels.Count() > 0) {
                builder.AppendLine(HtmlUtils.LINE_BREAK);
                foreach (Label label in property.Labels)
                    builder.AppendLine(string.Format("{0}{1}: {2}",
                        HtmlUtils.LINE_BREAK,
                        label.Name,
                        label.Value));
            }

            string toolTip = builder.ToString();
            if (toolTip.Length > MAX_TOOLTIP_LENGTH) {
                toolTip = toolTip.Substring(0, MAX_TOOLTIP_LENGTH);
                toolTip += "... (Truncated)";
            }

            return toolTip;
        }

        private string CreateModelToolTip(Model model) {
            StringBuilder builder = new();
            Schema schema = Schema.Singleton;

            for (int ii = 0; ii < model.Levels.Length; ii++)
                AddLabelToToolTip(builder, schema.GetLevelName(ii), model.GetLevel(ii));
            builder.AppendLine(HtmlUtils.LINE_BREAK);

            AddLabelToToolTip(builder, "Name", model.Name);

            foreach (Label label in model.Labels)
                AddLabelToToolTip(builder, label.Name, label.Value);

            if (!string.IsNullOrWhiteSpace(model.Description)) {
                builder.AppendLine(HtmlUtils.LINE_BREAK);
                builder.AppendLine(model.Description);
            }

            return builder.ToString();
        }

        private void AddLabelToToolTip(StringBuilder builder, string label, string value) {
            if (!string.IsNullOrEmpty(value))
                builder.AppendLine(label + ": " + value);
        }

        private static string ToShortType(Property property) {
            // Potential to inject shortened data types here...
            return property.DataType;
        }
        #endregion

        #region Edges / Associations
        private Edge AssociationToEdge(Association association) {
            Edge edge = new() {
                Source = ModelToNodeId(association.OtherSideModel),
                Destination = association.IsPolymorphic ?
                    association.PolymorphicName :
                    ModelToNodeId(association.OwnerSideModel),
                Association = association,
            };

            edge.SetAttrGraph("dir", "both")        // Allows for both ends of line to be decorated
                .SetAttrGraph("arrowsize", 1.5)     // I wanted to make this larger but the arrow icons overlap
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("arrowtail", MultiplicityToArrowName(association.OtherMultiplicity))
                .SetAttrGraph("arrowhead", MultiplicityToArrowName(association.OwnerMultiplicity))
                .SetAttrGraph("edgetooltip", ToEdgeToolTip(association))
                .SetAttrGraph("edgehref", association.DocUrl);

            SetRole(edge, "taillabel", association.InterestingOtherRole);
            SetRole(edge, "headlabel", association.InterestingOwnerRole);

            return edge;
        }

        private string ToEdgeToolTip(Association association) {
            StringBuilder builder = new();

            builder.AppendLine(string.Format("{0}.{1}", association.OwnerSideModel.HumanName, association.RefProperty.HumanName));
            builder.Append(association.Description);

            return builder.ToString();
        }

        private void SetRole(Edge edge, string property, string role) {
            if (role == null)
                return;
            edge.SetAttrGraph(property, role);
        }

        private string MultiplicityToArrowName(Multiplicity multiplicity) {
            switch (multiplicity) {
                case Multiplicity.One: return "none";
                case Multiplicity.Many: return "crow";
                case Multiplicity.ZeroOrOne: return "odottee";
                case Multiplicity.Aggregation: return "odiamond";
                default:
                    throw new Exception("Unexpected multiplicity: " + multiplicity);
            }
        }
        #endregion

        #region Misc
        private static string ModelToNodeId(Model model) {
            return model.QualifiedName;
        }
        #endregion
    }
}
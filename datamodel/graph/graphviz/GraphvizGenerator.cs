using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema;
using datamodel.graphviz.dot;
using datamodel.utils;
using datamodel.toplevel;

namespace datamodel.graphviz {

    public class GraphvizGenerator {

        #region Top-Level
        public void GenerateGraph(
            GraphDefinition graphDef,
            IEnumerable<Model> tables,
            IEnumerable<Association> associations,
            IEnumerable<Model> extraModels,
            List<PolymorphicInterface> interfaces
            ) {

            Graph graph = CreateGraph(tables, associations, extraModels, interfaces)
                .SetAttrGraph("margin", "0.5")
                .SetAttrGraph("notranslate", true);

            if (graphDef.Sep != null)
                graph.SetAttrGraph("sep", "+" + graphDef.Sep.Value);

            if (graphDef.Len != null)
                graph.SetAttrGraph("len", graphDef.Len.Value);

            string baseName = graphDef.FullyQualifiedName;
            string dotPath = Path.Combine(Env.TEMP_DIR, baseName + ".dot");

            using (TextWriter writer = new StreamWriter(dotPath))
                graph.ToDot(writer);

            string svgPath = Path.Combine(Env.OUTPUT_ROOT_DIR, baseName + ".svg");
            GraphvizRunner.Run(dotPath, svgPath, graphDef.Style);
        }

        public Graph CreateGraph(IEnumerable<Model> tables, IEnumerable<Association> associations, IEnumerable<Model> extraModels, List<PolymorphicInterface> interfaces) {
            Graph graph = new Graph();
            // Graphviz forces the images to be available on disk, even though they are not needed for SVG
            // This means that the build path to the imsages has to be the same as the web deploy path, which is annoying
            // I've left the following line commented out in case this is ever needed.
            //.SetAttrGraph("imagepath", IMAGE_PATH);

            IEnumerable<Model> allModels = tables.Union(extraModels);

            foreach (Model table in tables)
                graph.AddNode(ModelToNode(allModels, table));

            foreach (Model table in extraModels)
                graph.AddNode(ExtraModelToNode(null, table));

            foreach (Association association in associations)
                graph.AddEdge(AssociationToEdge(association));

            foreach (PolymorphicInterface _interface in interfaces) {
                graph.AddNode(PolymorphicInterfaceToNode(_interface));
                graph.AddEdge(PolymorphicInterfaceToEdge(_interface));
            }

            return graph;
        }
        #endregion

        #region Polymorphic Associations
        private Node PolymorphicInterfaceToNode(PolymorphicInterface _interface) {
            Node node = new Node() {
                Name = _interface.Name,
            };

            node.SetAttrGraph("tooltip", "Polymorphic Interface: " + _interface.Name)
                .SetAttrGraph("shape", "circle")
                .SetAttrGraph("label", "PMI")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("margin", 0);

            return node;
        }

        private Edge PolymorphicInterfaceToEdge(PolymorphicInterface _interface) {
            Edge edge = new Edge() {
                Source = ModelToNodeId(_interface.Model),
                Destination = _interface.Name,
            };

            edge.SetAttrGraph("arrowhead", "none");

            return edge;
        }
        #endregion

        #region Models
        private Node ModelToNode(IEnumerable<Model> tables, Model table) {
            Node node = new Node() {
                Name = ModelToNodeId(table),
            };

            node.SetAttrGraph("style", "filled")
                .SetAttrGraph("fillcolor", "pink")
                .SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("label", CreateLabel(tables, table, true));

            return node;
        }

        #endregion

        #region Extra Models - Not part of graph but added as "glue"
        private Node ExtraModelToNode(IEnumerable<Model> tables, Model table) {
            Node node = new Node() {
                Name = ModelToNodeId(table),
            };

            node.SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("height", 1.0)                // Minimum height in inches... Allows for more connections
                .SetAttrGraph("label", CreateLabel(tables, table, false));

            return node;
        }
        #endregion

        #region Common Node Creation
        private HtmlEntity CreateLabel(IEnumerable<Model> tables, Model dbModel, bool includeColumns) {

            HtmlTable table = new HtmlTable()
                .SetAttrHtml("border", 0)
                .SetAttrHtml("cellborder", 0)
                .SetAttrHtml("cellspacing", 0);

            // Header
            string headerText = HtmlUtils.SetFont(HtmlUtils.MakeBold(dbModel.HumanName), 16);

            HtmlTd headerTd = new HtmlTd(headerText)
                .SetAttrHtml("tooltip", string.IsNullOrEmpty(dbModel.Description) ? "No description provided" : dbModel.Description)
                .SetAttrHtml("href", UrlService.Singleton.DocUrl(dbModel));

            table.AddTr(new HtmlTr(headerTd));

            if (!includeColumns)
                return table;

            // Columns
            foreach (Column column in dbModel.AllColumns) {
                if (Schema.IsInteresting(column) &&
                    !column.Deprecated) {

                    HtmlTr row = new HtmlTr();

                    string columnName = HtmlUtils.Bullet() + column.HumanName;
                    HtmlTd columnNameTd = new HtmlTd();
                    row.AddTd(columnNameTd);

                    // Foreign Key Column
                    if (column.IsFk) {
                        Model referencedModel = column.FkInfo.ReferencedModel;
                        if (tables.Contains(referencedModel))
                            continue;           // Do not include FK column if in this graph... It will be shown via an association line
                        else {
                            columnNameTd.Text = columnName;

                            if (referencedModel != null) {
                                // TODO(bartekmi) Currently, we only show a link to the largest graph. Consider exposing links to all sizes.
                                row.AddTd(new HtmlTd(HtmlUtils.MakeImage(IconUtils.DIAGRAM_SMALL))
                                   .SetAttrHtml("tooltip", string.Format("Go to diagram which contains linked table: '{0}'", referencedModel.HumanName))
                                   .SetAttrHtml("href", UrlService.Singleton.GetGraphs(referencedModel).First().SvgUrl));

                                row.AddTd(new HtmlTd(HtmlUtils.MakeImage(IconUtils.DOCS_SMALL))
                                   .SetAttrHtml("tooltip", string.Format("Go to Data Dictionary of linked table: '{0}'", referencedModel.HumanName))
                                   .SetAttrHtml("href", UrlService.Singleton.DocUrl(referencedModel)));
                            }
                        }
                    }
                    // Regular Column
                    else {
                        string dataType = string.Format("<FONT COLOR=\"gray50\">({0})</FONT>", ToShortType(column));
                        columnNameTd.Text = string.Format("{0} {1}", columnName, dataType);
                    }

                    // Attributes for the column name Html-like element
                    columnNameTd
                        .SetAttrHtml("align", "left")
                        .SetAttrHtml("tooltip", string.IsNullOrEmpty(column.Description) ?
                            string.Format("Go to Data Dictionary for Column '{0}'", column.HumanName) :
                            column.DescriptionParagraphs.First())
                        .SetAttrHtml("href", column.DocUrl);

                    table.AddTr(row);
                }
            }

            return table;
        }

        private string ToShortType(Column column) {
            switch (column.DbType) {
                case DataType.Integer: return "int";
                case DataType.Text: return "txt";
                case DataType.String: return "str";
                case DataType.Decimal: return "dec";
                case DataType.Boolean: return "bool";
            }

            return column.DbTypeString;
        }
        #endregion

        #region Edges / Associations
        private Edge AssociationToEdge(Association association) {
            Edge edge = new Edge() {
                Source = ModelToNodeId(association.OtherSideModel),
                Destination = association.IsPolymorphic ?
                    association.OtherSidePolymorphicName :
                    ModelToNodeId(association.FkSideModel),
                Association = association,
            };

            edge.SetAttrGraph("dir", "both")        // Allows for both ends of line to be decorated
                .SetAttrGraph("arrowsize", 1.5)     // I wanted to make this larger but the arrow icons overlap
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("arrowtail", MultiplicityToArrowName(association.OtherSideMultiplicity))
                .SetAttrGraph("arrowhead", MultiplicityToArrowName(association.FkSideMultiplicity))
                .SetAttrGraph("edgetooltip", edge.Association.Description)
                .SetAttrGraph("edgehref", edge.Association.DocUrl);

            string oppositeFK = association.RoleOppositeFK;
            if (oppositeFK != null)
                edge.SetAttrGraph("taillabel", oppositeFK.Replace(" ", "\n"));

            string byFK = association.RoleByFK;
            if (byFK != null)
                edge.SetAttrGraph("headlabel", byFK.Replace(" ", "\n"));

            return edge;
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
        private static string ModelToNodeId(Model table) {
            return table.SanitizedClassName.Replace(':', '_');
        }
        #endregion
    }
}
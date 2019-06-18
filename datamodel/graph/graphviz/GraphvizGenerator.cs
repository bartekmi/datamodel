using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema;
using datamodel.graphviz.dot;

namespace datamodel.graphviz {

    public class GraphvizGenerator {

        #region Top-Level
        public void GenerateGraph(string path, IEnumerable<Table> tables, IEnumerable<Association> associations) {
            Graph graph = CreateGraph(tables, associations)
                .SetAttrGraph("margin", "0.5");

            using (TextWriter writer = new StreamWriter(path))
                graph.ToDot(writer);
        }

        public Graph CreateGraph(IEnumerable<Table> tables, IEnumerable<Association> associations) {
            Graph graph = new Graph();
            // Graphviz forces the images to be available on disk, even though they are not needed for SVG
            // This means that the build path to the imsages has to be the same as the web deploy path, which is annoying
            // I've left the following line commented out in case this is ever needed.
            //.SetAttrGraph("imagepath", IMAGE_PATH);

            foreach (Table table in tables)
                graph.AddNode(ConvertTable(tables, table));

            foreach (Association association in associations)
                graph.AddEdge(ConvertAssociation(association));

            return graph;
        }
        #endregion

        #region Tables
        private Node ConvertTable(IEnumerable<Table> tables, Table table) {
            Node node = new Node() {
                Name = TableToNodeId(table),
            };

            node.SetAttrGraph("style", "filled")
                .SetAttrGraph("fillcolor", "pink")
                .SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("label", CreateLabel(tables, table));

            return node;
        }

        private HtmlEntity CreateLabel(IEnumerable<Table> tables, Table dbTable) {

            HtmlTable table = new HtmlTable()
                .SetAttrHtml("border", 0)
                .SetAttrHtml("cellborder", 0)
                .SetAttrHtml("cellspacing", 0);

            // Header
            string headerText = HtmlUtils.SetFont(HtmlUtils.MakeBold(dbTable.HumanName), 16);

            HtmlTd headerTd = new HtmlTd(headerText)
                .SetAttrHtml("tooltip", string.IsNullOrEmpty(dbTable.Description) ? "No description provided" : dbTable.Description)
                .SetAttrHtml("href", dbTable.DocUrl);

            table.AddTr(new HtmlTr(headerTd));

            // Columns
            foreach (Column column in dbTable.AllColumns) {
                if (Schema.IsInteresting(column)) {
                    HtmlTr row = new HtmlTr();

                    string columnName = HtmlUtils.Bullet() + column.HumanName;
                    HtmlTd columnNameTd = new HtmlTd();
                    row.AddTd(columnNameTd);

                    if (column.IsFk) {
                        Table referencedTable = column.FkInfo.ReferencedTable;
                        if (tables.Contains(referencedTable))
                            continue;           // Do not include FK column if in this graph... It will be shown via an association line
                        else {
                            columnNameTd.Text = columnName;
                            if (referencedTable != null) {
                                HtmlTd externalLinkTd = new HtmlTd("<IMG SRC=\"/datamodel/assets/images/external-link-blue.png\"/>")
                                   .SetAttrHtml("tooltip", "Jump to the diagram which contains the linked table")
                                   .SetAttrHtml("href", referencedTable.SvgUrl);
                                row.AddTd(externalLinkTd);
                            }
                        }
                    } else {
                        string dataType = string.Format("<FONT COLOR=\"gray50\">({0})</FONT>", ToShortType(column));
                        columnNameTd.Text = columnName + dataType;
                    }

                    columnNameTd
                        .SetAttrHtml("align", "left")
                        .SetAttrHtml("tooltip", string.IsNullOrEmpty(column.Description) ? "No description provided" : column.Description)
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
        private Edge ConvertAssociation(Association association) {
            Edge edge = new Edge() {
                Source = TableToNodeId(association.SourceTable),
                Destination = TableToNodeId(association.DestinationTable),
            };

            edge.SetAttrGraph("dir", "both")        // Allows for both ends of line to be decorated
                .SetAttrGraph("arrowsize", 1.5)
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("arrowtail", MultiplicityToArrowName(association.SourceMultiplicity))
                .SetAttrGraph("arrowhead", MultiplicityToArrowName(association.DestinationMultiplicity));

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
        private static string TableToNodeId(Table table) {
            return table.DbName;
        }
        #endregion
    }
}
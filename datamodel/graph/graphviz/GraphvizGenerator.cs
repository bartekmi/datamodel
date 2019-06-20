using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema;
using datamodel.graphviz.dot;
using datamodel.utils;
using datamodel.graph;

namespace datamodel.graphviz {

    public class GraphvizGenerator {

        #region Top-Level
        public void GenerateGraph(GraphDefinition graphDef, string path, IEnumerable<Table> tables, IEnumerable<Association> associations, IEnumerable<Table> extraTables) {
            Graph graph = CreateGraph(tables, associations, extraTables)
                .SetAttrGraph("margin", "0.5")
                .SetAttrGraph("notranslate", true);

            if (graphDef.Sep != null)
                graph.SetAttrGraph("sep", "+" + graphDef.Sep.Value);

            if (graphDef.Len != null)
                graph.SetAttrGraph("len", graphDef.Len.Value);

            using (TextWriter writer = new StreamWriter(path))
                graph.ToDot(writer);
        }

        public Graph CreateGraph(IEnumerable<Table> tables, IEnumerable<Association> associations, IEnumerable<Table> extraTables) {
            Graph graph = new Graph();
            // Graphviz forces the images to be available on disk, even though they are not needed for SVG
            // This means that the build path to the imsages has to be the same as the web deploy path, which is annoying
            // I've left the following line commented out in case this is ever needed.
            //.SetAttrGraph("imagepath", IMAGE_PATH);

            IEnumerable<Table> allTables = tables.Union(extraTables);

            foreach (Table table in tables)
                graph.AddNode(TableToNode(allTables, table));

            foreach (Table table in extraTables)
                graph.AddNode(ExtraTableToNode(null, table));

            foreach (Association association in associations)
                graph.AddEdge(AssociationToEdge(association));

            return graph;
        }
        #endregion

        #region Tables
        private Node TableToNode(IEnumerable<Table> tables, Table table) {
            Node node = new Node() {
                Name = TableToNodeId(table),
            };

            node.SetAttrGraph("style", "filled")
                .SetAttrGraph("fillcolor", "pink")
                .SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("label", CreateLabel(tables, table, true));

            return node;
        }

        #endregion

        #region Extra Tables - Not part of graph but added as "glue"
        private Node ExtraTableToNode(IEnumerable<Table> tables, Table table) {
            Node node = new Node() {
                Name = TableToNodeId(table),
            };

            node.SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("height", 1.0)                // Minimum height in inches... Allows for more connections
                .SetAttrGraph("label", CreateLabel(tables, table, false));

            return node;
        }
        #endregion

        #region Common Node Creation
        private HtmlEntity CreateLabel(IEnumerable<Table> tables, Table dbTable, bool includeColumns) {

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

            if (!includeColumns)
                return table;

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
                                row.AddTd(new HtmlTd(HtmlUtils.MakeImage(IconUtils.DIAGRAM_SMALL))
                                   .SetAttrHtml("tooltip", string.Format("Go to diagram which contains linked table: '{0}'", referencedTable.HumanName))
                                   .SetAttrHtml("href", referencedTable.SvgUrl));

                                row.AddTd(new HtmlTd(HtmlUtils.MakeImage(IconUtils.DOCS_SMALL))
                                   .SetAttrHtml("tooltip", string.Format("Go to Data Dictionary of linked table: '{0}'", referencedTable.HumanName))
                                   .SetAttrHtml("href", referencedTable.DocUrl));
                            }
                        }
                    } else {
                        string dataType = string.Format("<FONT COLOR=\"gray50\">({0})</FONT>", ToShortType(column));
                        columnNameTd.Text = columnName + dataType;
                    }

                    columnNameTd
                        .SetAttrHtml("align", "left")
                        .SetAttrHtml("tooltip", string.IsNullOrEmpty(column.Description) ?
                            string.Format("Go to Data Dictionary for Column '{0}'", column.HumanName) :
                            column.Description)
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
                Source = TableToNodeId(association.SourceTable),
                Destination = TableToNodeId(association.DestinationTable),
                Association = association,
            };

            edge.SetAttrGraph("dir", "both")        // Allows for both ends of line to be decorated
                .SetAttrGraph("arrowsize", 1.5)     // I wanted to make this larger but the arrow icons overlap
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("arrowtail", MultiplicityToArrowName(association.SourceMultiplicity))
                .SetAttrGraph("arrowhead", MultiplicityToArrowName(association.DestinationMultiplicity))
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
        private static string TableToNodeId(Table table) {
            return table.DbName;
        }
        #endregion
    }
}
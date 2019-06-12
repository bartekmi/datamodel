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
            Graph graph = CreateGraph(tables, associations);

            using (TextWriter writer = new StreamWriter(path))
                graph.ToDot(writer);
        }

        public Graph CreateGraph(IEnumerable<Table> tables, IEnumerable<Association> associations) {
            Graph graph = new Graph();

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
                .SetAttrGraph("fontname", "Helvetica")
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
                .SetAttrHtml("href", CreateLink(dbTable, null));

            table.AddTr(new HtmlTr(headerTd));

            // Columns
            foreach (Column column in dbTable.AllColumns) {
                if (Schema.IsInteresting(column)) {
                    string dataType = string.Format("<FONT COLOR=\"gray50\">({0})</FONT>", ToShortType(column));
                    string content = HtmlUtils.Bullet() + column.HumanName;

                    if (column is FkColumn) {
                        FkColumn fkColumn = (FkColumn)column;
                        if (tables.Contains(fkColumn.ReferencedTable))
                            continue;           // Do not include FK column if in this graph... It will be shown via an association line
                        else
                            content += "***";   // Treat FK's to external tables very much like regular columns but add icon
                    } else
                        content += dataType;

                    HtmlTd td = new HtmlTd(content)
                        .SetAttrHtml("align", "left")
                        .SetAttrHtml("tooltip", string.IsNullOrEmpty(column.Description) ? "No description provided" : column.Description)
                        .SetAttrHtml("href", CreateLink(dbTable, column));
                    table.AddTr(new HtmlTr(td));
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

        private string CreateLink(Table table, Column column) {
            string filename = table.ClassName + ".html";
            string urlToEntity = Path.Combine(Program.DATA_DICTIONARY_DIR, table.Team, filename);

            if (column == null)
                return urlToEntity;
            else
                return string.Format("{0}#{1}", urlToEntity, column.DbName);
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
                .SetAttrGraph("arrowtail", MultiplicityToArrowName(association.SourceMultiplicity))
                .SetAttrGraph("arrowhead", MultiplicityToArrowName(association.DestinationMultiplicity));

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
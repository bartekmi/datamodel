using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema;
using datamodel.graphviz.dot;

namespace datamodel.graphviz {
    public class GraphGenerator {
        public void GenerateGraph(string path, IEnumerable<Table> tables) {
            Graph graph = CreateGraph(tables);

            using (TextWriter writer = new StreamWriter(path))
                graph.ToDot(writer);
        }

        public Graph CreateGraph(IEnumerable<Table> tables) {
            Graph graph = new Graph();

            foreach (Table table in tables)
                graph.AddNode(ConvertTable(table));

            return graph;
        }

        private Node ConvertTable(Table dbTable) {
            Node node = new Node() {
                Name = dbTable.DbName,
            };

            node.SetAttrGraph("style", "filled")
                .SetAttrGraph("fillcolor", "pink")
                .SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")
                .SetAttrGraph("label", CreateLabel(dbTable));

            return node;
        }

        private HtmlEntity CreateLabel(Table dbTable) {

            HtmlTable table = new HtmlTable()
                .SetAttrHtml("border", 0)
                .SetAttrHtml("cellborder", 0)
                .SetAttrHtml("cellspacing", 0);

            // Header
            string headerText = HtmlUtils.SetFont(HtmlUtils.MakeBold(dbTable.ClassName), 16);

            HtmlTd headerTd = new HtmlTd(headerText)
                .SetAttrHtml("tooltip", string.IsNullOrEmpty(dbTable.Description) ? "No description provided" : dbTable.Description)
                .SetAttrHtml("href", CreateLink(dbTable, null));

            table.AddTr(new HtmlTr(headerTd));

            // Columns
            foreach (Column column in dbTable.RegularColumns) {
                if (Schema.IsInteresting(column)) {
                    HtmlTd td = new HtmlTd(HtmlUtils.Bullet() + column.HumanName)
                        .SetAttrHtml("align", "left")
                        .SetAttrHtml("tooltip", string.IsNullOrEmpty(column.Description) ? "No description provided" : column.Description)
                        .SetAttrHtml("href", CreateLink(dbTable, column));
                    table.AddTr(new HtmlTr(td));
                }
            }

            return table;
        }

        private string CreateLink(Table table, Column column) {
            string filename = table.ClassName + ".html";
            string urlToEntity = Path.Combine(Program.DATA_DICTIONARY_DIR, table.Team, filename);

            if (column == null)
                return urlToEntity;
            else
                return string.Format("{0}#{1}", urlToEntity, column.DbName);
        }
    }
}
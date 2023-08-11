using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using datamodel.graphviz.dot;
using datamodel.toplevel;
using datamodel.schema;
using datamodel.metadata;
using datamodel.utils;

namespace datamodel.graphviz {
    // This class generates the index in the form of a Graphviz graph
    // The particulars:
    // - Use the same hierarchy tree as for generating the HTML index.
    // - To limit clutter, only show nodes of color
    // - Nodes:
    //      - Name of the node at each level
    //      - Link to associate diagram
    //      - Node count
    //      - Set color as per HierarchyItem
    //      - Tooltip contains list of Models
    // - Edges:
    //      - Link nodes that have associations
    //      - Line thickness corresponds to association count
    //      - Direction corresponds to Owner direction. Can have arrow on both ends.
    //      - Link tool-tip lists associations
    public static class GraphvizIndexGenerator {

        #region Top Level
        public static void GenerateIndex(HierarchyItem root) {

            // HierarchyItem.DebugPrint(root);

            Graph graph = new Graph()
                .SetAttrGraph("pad", "0.5")
                .SetAttrGraph("overlap", "false")
                .SetAttrGraph("notranslate", true);


            AddNodesRecursive(graph, root);
            AddAssociations(graph);    // Edges for top-level graph


            string baseName = "index";
            GraphvizRunner.CreateDotAndRun(graph, baseName, RenderingStyle.Fdp);
        }

        private static void AddNodesRecursive(
            Graph graph,
            HierarchyItem hItem) {

            if (hItem.ColorString != null)
                graph.AddNode(ToNode(hItem));

            foreach (HierarchyItem grandchild in hItem.Children)
                AddNodesRecursive(graph, grandchild);
        }
        #endregion

        #region Associations

        private static void AddAssociations(Graph graph) {
            Dictionary<string, AggregatedAssociation> aggregatedAssociations = new();

            // First, iterate all associations and aggregate associations between colored HierarchyItems
            foreach (Association association in Schema.Singleton.Associations) {
                HierarchyItem from = FindColoredAncestor(association.OwnerSideModel);
                HierarchyItem to = FindColoredAncestor(association.OtherSideModel);

                if (from == null || to == null || from == to)
                    continue;

                string key = AggregatedAssociation.CreateKey(from, to);
                if (!aggregatedAssociations.TryGetValue(key, out AggregatedAssociation aa)) {
                    aa = new AggregatedAssociation() {
                        From = from,
                        To = to,
                    };
                    aggregatedAssociations[key] = aa;
                }
                aa.AddAssociation(association, to);
            }

            // Second, actually add the Edges
            foreach (AggregatedAssociation aa in aggregatedAssociations.Values)
                graph.AddEdge(ToEdge(aa));
        }

        private static HierarchyItem FindColoredAncestor(Model model) {
            if (model == null)
                return null;

            HierarchyItem item = model.LeafHierachyItem;
            while (item != null) {
                if (item.ColorString != null)
                    return item;
                item = item.Parent;
            }

            return null;
        }

        private static Edge ToEdge(AggregatedAssociation aa) {
            Edge edge = new() {
                Source = HI_ToNodeId(aa.From),
                Destination = HI_ToNodeId(aa.To),
            };

            edge.SetAttrGraph("dir", "both")        // Allows for both ends of line to be decorated
                .SetAttrGraph("arrowsize", 1.5)     // I wanted to make this larger but the arrow icons overlap
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("tooltip", CreateEdgeToolTip(aa))
                .SetAttrGraph("arrowhead", "normal")
                .SetAttrGraph("penwidth", 4.0)
                .SetAttrGraph("color", GetColorForAssociationCount(aa.Associations.Count))
                .SetAttrGraph("arrowtail", aa.IncludeReverseArrow ? "normal" : "none");

            edge.SetAttrGraph("ltail", HI_ToNodeId(aa.From));
            if (aa.IncludeReverseArrow)
                edge.SetAttrGraph("lhead", HI_ToNodeId(aa.To));

            return edge;
        }

        // Get a gray-scale color, with 1 being the lightest, and certain max value (and above) being black
        private static string GetColorForAssociationCount(int count) {
            int lightest = 220;
            int darkest = 40;

            // double intensityFract = Math.Min((count - minCount) / (maxCount - minCount), 1.0);
            double intensityFract = Math.Min(1.0, Math.Log(count, 2.0) / 5.0);        // 2^5 = 32 maps to 1.0 (full black)
            int intensityAbs = (int)(lightest - (lightest - darkest) * intensityFract);

            string intensityString = intensityAbs.ToString("X2");

            return string.Format("#{0}{0}{0}", intensityString);
        }

        private static string CreateEdgeToolTip(AggregatedAssociation aa) {
            StringBuilder builder = new();
            builder.AppendLine(string.Format("Arrow(s) show direction of References's{0}", HtmlUtils.LINE_BREAK));
            builder.AppendLine(string.Format("{0} References: {1}", aa.Associations.Count, HtmlUtils.LINE_BREAK));

            var groups = aa.Associations.GroupBy(x => x.ToString()).ToDictionary(x => x.Key);
            IEnumerable<string> associations = aa.Associations
                .OrderBy(x => x.ToString())
                .Select(x => string.Format("{0} {1}{2} => {3}",
                    HtmlUtils.ASTERISK,
                    x.OwnerSideModel.HumanName,
                    groups[x.ToString()].Count() > 1 ? string.Format(" ({0})", x.OtherRole) : "",  // Disambiguate identical model pairs
                    x.OtherSideModel.HumanName));

            builder.AppendLine(string.Join(HtmlUtils.LINE_BREAK, associations));

            return builder.ToString();
        }

        internal class AggregatedAssociation {
            internal HierarchyItem From;
            internal HierarchyItem To;
            internal List<Association> Associations = new();
            internal bool IncludeReverseArrow;

            internal void AddAssociation(Association association, HierarchyItem to) {
                Associations.Add(association);
                if (From == to)
                    IncludeReverseArrow = true;
            }

            internal static string CreateKey(HierarchyItem from, HierarchyItem to) {
                IEnumerable<string> ordered = new string[] { from.UniqueName, to.UniqueName }.OrderBy(x => x);
                return string.Join("|", ordered);
            }
        }

        #endregion

        #region Node Creation
        private static Node ToNode(HierarchyItem item) {
            Node node = new() {
                Name = HI_ToNodeId(item),
            };

            // Through painful trial and error, I learned that hyperlinks and tooltips
            // only work on <td> elements and nodes, but not on <table> or <tr> elements
            node.SetAttrGraph("style", "filled")
                .SetAttrGraph("fillcolor", item.ColorString)
                .SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("href", item.Graph.GetSvgUrl(false))
                .SetAttrGraph("tooltip", CreateNodeToolTip(item))
                .SetAttrGraph("label", CreateLabel(item));

            return node;
        }

        private static HtmlEntity CreateLabel(HierarchyItem item) {
            HtmlTable table = new HtmlTable()
                .SetAttrHtml("border", 0)
                .SetAttrHtml("cellborder", 0)
                .SetAttrHtml("cellspacing", 0);

            List<HierarchyItem> parentage = new();
            HierarchyItem pointer = item;
            while (pointer.Parent != null) {
                parentage.Insert(0, pointer);
                pointer = pointer.Parent;
            }

            Schema schema = Schema.Singleton;

            foreach (HierarchyItem level in parentage) {
                string label = schema.GetLevelName(level.Level - 1) + ": ";

                table.AddTr(new HtmlTr(
                    new HtmlTd(label),
                    new HtmlTd(string.IsNullOrWhiteSpace(level.Name) ? "None" : level.Name)
                ));
            }

            table.AddTr(new HtmlTr(
                new HtmlTd(""),
                new HtmlTd(string.Format("{0} Models", item.ModelCount))
            ));

            return table;
        }

        private static string CreateNodeToolTip(HierarchyItem item) {
            IEnumerable<string> models = item.Models
                .OrderBy(x => x.HumanName)
                .Select(x => string.Format("{0} {1}", HtmlUtils.ASTERISK, x.HumanName));

            return string.Format("Models: {0}{0}{1}",
                HtmlUtils.LINE_BREAK,
                string.Join(HtmlUtils.LINE_BREAK, models));
        }
        #endregion

        #region Utils

        private static string HI_ToNodeId(HierarchyItem item) {
            return NameUtils.CompoundToSafe(item.CumulativeName);
        }

        #endregion
    }
}
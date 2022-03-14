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
    // - Put a lower limit on nodes that have too few leaf children to avoid cluttering the graph
    // - Use a subgraph for each nested level
    // - Nodes:
    //      - Name of the node: Level1, Leve2, Level3
    //          - Links to diagram
    //          - Tooltip contains list of leaf nodes (by human name, alphabetical)
    //      - Type of node (Level1, Leve2, Level3)
    //      - Leaf count
    //      - (NTH) Background color according to Level1
    // - Edges:
    //      - Link all entities that have associations of any kind
    //      - Links are at the highest level possible (a link would never go through a bounding-box of its sub-graph) 
    //      - (NTH) Line thickness corresponds to association count
    //      - (NTH) Direction corresponds to Ref direction. Can have arrow on both ends.
    //      - (NTH) Link tool-tip lists associations
    public static class GraphvizIndexGenerator {


        #region Top Level
        public static void GenerateIndex(HierarchyItem root) {

            // HierarchyItem.DebugPrint(root);

            Dictionary<Model, HierarchyItem> modelToHI = new Dictionary<Model, HierarchyItem>();
            HierarchyItem.Recurse(root, (hi) => {
                if (hi.IsLeaf)
                    foreach (Model model in hi.Models)
                        modelToHI[model] = hi;
            });

            Graph graph = new Graph()
                .SetAttrGraph("compound", true)
                .SetAttrGraph("pad", "0.5")
                .SetAttrGraph("K", "0.02")          // Reduce from default 0.3 to try to make graph tighter
                .SetAttrGraph("nodesep", "1")
                .SetAttrGraph("ranksep", "1")
                .SetAttrGraph("overlap", "false")
                .SetAttrGraph("notranslate", true);


            // Note that top-level children are treated differently, in that a subgraph
            // is always created, regadless of number of child models
            foreach (HierarchyItem child in root.Children)
                GenerateIndexRecursive(modelToHI, graph, child);
            AddAssociations(modelToHI, graph, root);    // Edges for top-level graph


            string baseName = "index";
            GraphvizRunner.CreateDotAndRun(graph, baseName, RenderingStyle.Fdp);
        }

        private static void GenerateIndexRecursive(
            Dictionary<Model, HierarchyItem> modelToHI,
            GraphBase parent,
            HierarchyItem childItem) {

            if (ShowAsSubgraph(childItem)) {
                Subgraph subgraph = ToSubgraph(childItem);
                parent.AddSubgraph(subgraph);
                foreach (HierarchyItem grandchild in childItem.Children)
                    if (grandchild.ShouldShowOnIndex)
                        GenerateIndexRecursive(modelToHI, subgraph, grandchild);
                AddAssociations(modelToHI, subgraph, childItem);    // Edges for subgraph
            } else {
                parent.AddNode(ToNode(childItem));
            }
        }
        #endregion

        #region Associations
        private static void AddAssociations(
            Dictionary<Model, HierarchyItem> modelToHI,
            GraphBase graph,
            HierarchyItem item) {

            Dictionary<string, AggregatedAssociation> aggregatedAssociations = new Dictionary<string, AggregatedAssociation>();

            // First iteration is to collect associations that should be drawn for this level and aggregate them,
            // since we don't want to draw multiple lines between same subgraphs
            foreach (HierarchyItem childItem in item.Children.Where(x => x.ShouldShowOnIndex)) {
                foreach (Model model in childItem.Models) {
                    foreach (Association association in model.RefAssociations.Where(x => x.OtherSideModel != null)) {
                        HierarchyItem otherSide = modelToHI[association.OtherSideModel];
                        HierarchyItem otherSideSibling = otherSide.FindAncestorAtLevel(childItem.Level);
                        if (otherSideSibling == null ||                      // No sibling
                            otherSideSibling.Parent != childItem.Parent ||   // Other side not in direct parent
                            otherSideSibling == childItem ||                 // Other side in same subgraph
                            !otherSideSibling.ShouldShowOnIndex)             // Too few models to bother showing
                            continue;

                        string key = AggregatedAssociation.CreateKey(childItem, otherSideSibling);
                        if (!aggregatedAssociations.TryGetValue(key, out AggregatedAssociation aa)) {
                            aa = new AggregatedAssociation();
                            aggregatedAssociations[key] = aa;
                        }
                        aa.AddAssociation(association, childItem, otherSideSibling);
                    }
                }
            }

            // Second iteration is to actually add the Edges
            foreach (AggregatedAssociation aa in aggregatedAssociations.Values)
                graph.AddEdge(ToEdge(aa));
        }

        private static Edge ToEdge(AggregatedAssociation aa) {
            Edge edge = new Edge() {
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

            if (ShowAsSubgraph(aa.From))
                edge.SetAttrGraph("ltail", HI_ToNodeId(aa.From));
            if (ShowAsSubgraph(aa.To))
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
            StringBuilder builder = new StringBuilder();
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
            internal List<Association> Associations = new List<Association>();
            internal bool IncludeReverseArrow;

            internal void AddAssociation(Association association, HierarchyItem from, HierarchyItem to) {
                Associations.Add(association);
                if (From == null) {
                    From = from;
                    To = to;
                } else if (From == to)
                    IncludeReverseArrow = true;
            }

            internal static string CreateKey(HierarchyItem from, HierarchyItem to) {
                IEnumerable<string> ordered = new string[] { from.UniqueName, to.UniqueName }.OrderBy(x => x);
                return string.Join("|", ordered);
            }
        }

        #endregion

        #region Subgraphs and Nodes
        private static Subgraph ToSubgraph(HierarchyItem item) {
            Subgraph subgraph = new Subgraph() {
                Name = HI_ToNodeId(item),
            };

            subgraph.SetAttrGraph("pencolor", "black")
                    .SetAttrGraph("label", string.Format("{0} ({1} models)", item.HumanName, item.ModelCount))
                    .SetAttrGraph("href", item.Graph.SvgUrl)
                    .SetAttrGraph("fontname", "Helvetica");

            return subgraph;
        }

        private static Node ToNode(HierarchyItem item) {
            Node node = new Node() {
                Name = HI_ToNodeId(item),
            };

            // Through painful trial and error, I learned that hyperlinks and tooltips
            // only work on <td> elements and nodes, but not on <table> or <tr> elements
            node.SetAttrGraph("style", "filled")
                .SetAttrGraph("fillcolor", item.ColorString)
                .SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("href", item.Graph.SvgUrl)
                .SetAttrGraph("tooltip", CreateNodeToolTip(item))
                .SetAttrGraph("label", CreateLabel(item));

            return node;
        }

        private static HtmlEntity CreateLabel(HierarchyItem item) {
            HtmlTable table = new HtmlTable()
                .SetAttrHtml("border", 0)
                .SetAttrHtml("cellborder", 0)
                .SetAttrHtml("cellspacing", 0);

            List<HierarchyItem> parentage = new List<HierarchyItem>();
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

        private static bool ShowAsSubgraph(HierarchyItem item) {
            return item.Children.Any(x => x.ModelCount >= Env.MIN_MODELS_TO_SHOW_AS_NODE);
        }

        private static string HI_ToNodeId(HierarchyItem item) {
            string uniqueName = NameUtils.CompoundToSafe(item.CumulativeName);
            if (ShowAsSubgraph(item))
                uniqueName = "cluster_" + uniqueName;
            return uniqueName;
        }

        // NOTE: This code only applies to 'dot' rendering style, but that style
        // produces undreadable output, so we don't use it.
        // When specifying node names in an edge, Graphviz does NOT allow specifying
        // the cluster name. Instead, you must link to a node within the cluster
        // and set the lhead/ltail attributes.
        // https://github.com/glejeune/Ruby-Graphviz/issues/35
        private static string HI_ToNodeIdInEdgeForDot(HierarchyItem item) {
            if (ShowAsSubgraph(item))
                return HI_ToNodeIdInEdgeForDot(item.Children.First(x => x.ShouldShowOnIndex));
            return HI_ToNodeId(item);
        }

        #endregion
    }
}
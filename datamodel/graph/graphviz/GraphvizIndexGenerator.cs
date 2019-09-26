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
    //      - Name of the node: Team, Engine, Module
    //          - Links to diagram
    //          - Tooltip contains list of leaf nodes (by human name, alphabetical)
    //      - Type of node (Team Engine Module)
    //      - Leaf count
    //      - (NTH) Background color according to team
    // - Edges:
    //      - Link all entities that have associations of any kind
    //      - Links are at the highest level possible (a link would never go through a bounding-box of its sub-graph) 
    //      - (NTH) Line thickness corresponds to association count
    //      - (NTH) Direction corresponds to FK direction. Can have arrow on both ends.
    //      - (NTH) Link tool-tip lists associations
    public static class GraphvizIndexGenerator {


        #region Top Level
        public static void GenerateIndex(HierarchyItem root) {

            HierarchyItem.DebugPrint(root);

            Dictionary<Model, HierarchyItem> modelToHI = new Dictionary<Model, HierarchyItem>();
            HierarchyItem.Recurse(root, (hi) => {
                if (hi.IsLeaf)
                    foreach (Model model in hi.Models)
                        modelToHI[model] = hi;
            });

            Graph graph = new Graph()
                .SetAttrGraph("compound", true)
                .SetAttrGraph("pad", "0.5")
                .SetAttrGraph("nodesep", "1")
                .SetAttrGraph("ranksep", "1")
                .SetAttrGraph("notranslate", true);


            // Note that top-level children are treated differently, in that a subgraph
            // is always created, regadless of number of child models
            foreach (HierarchyItem child in root.Children)
                GenerateIndexRecursive(modelToHI, graph, child);
            AddAssociations(modelToHI, graph, root);    // Edges for top-level graph


            string baseName = "index";
            GraphvizRunner.CreateDotAndRun(graph, baseName, RenderingStyle.Neato);
        }

        private static void GenerateIndexRecursive(
            Dictionary<Model, HierarchyItem> modelToHI,
            GraphBase parent,
            HierarchyItem childItem) {

            if (ShowAsSubgraph(childItem)) {
                Subgraph subgraph = ToSubgraph(childItem);
                parent.AddSubgraph(subgraph);
                foreach (HierarchyItem grandchild in childItem.Children)
                    if (grandchild.CumulativeModelCount >= Env.MIN_MODELS_TO_SHOW_AS_NODE)
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
            foreach (HierarchyItem childItem in item.Children) {
                foreach (Model model in childItem.CumulativeModels) {
                    foreach (Association association in model.FkAssociations.Where(x => x.OtherSideModel != null)) {
                        HierarchyItem otherSide = modelToHI[association.OtherSideModel];
                        HierarchyItem otherSideSibling = otherSide.FindAncestorAtLevel(childItem.Level);
                        if (otherSideSibling == null ||                      // No sibling
                            otherSideSibling.Parent != childItem.Parent ||   // Other side no in direct parent
                            otherSideSibling == childItem)                   // Other side in same subgraph
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
                Source = HI_ToNodeIdInEdge(aa.From),
                Destination = HI_ToNodeIdInEdge(aa.To),
            };

            edge.SetAttrGraph("dir", "both")        // Allows for both ends of line to be decorated
                .SetAttrGraph("arrowsize", 1.5)     // I wanted to make this larger but the arrow icons overlap
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("tooltip", CreateEdgeToolTip(aa))
                .SetAttrGraph("arrowhead", "normal")
                .SetAttrGraph("arrowtail", aa.IncludeReverseArrow ? "normal" : "none");

            if (ShowAsSubgraph(aa.From))
                edge.SetAttrGraph("ltail", HI_ToNodeId(aa.From));
            if (ShowAsSubgraph(aa.To))
                edge.SetAttrGraph("lhead", HI_ToNodeId(aa.To));

            return edge;
        }

        private static string CreateEdgeToolTip(AggregatedAssociation aa) {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("Arrow(s) show direction of FK's{0}{0}", HtmlUtils.LINE_BREAK));
            builder.AppendLine(string.Format("{0} Foreign keys: {1}{1}", aa.Associations.Count, HtmlUtils.LINE_BREAK));

            foreach (Association association in aa.Associations)
                builder.AppendLine(association + HtmlUtils.LINE_BREAK);

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
                IEnumerable<string> ordered = new string[] { from.Name, to.Name }.OrderBy(x => x);
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
                    .SetAttrGraph("label", item.HumanName)
                    .SetAttrGraph("fontname", "Helvetica");

            return subgraph;
        }

        private static Node ToNode(HierarchyItem item) {
            Node node = new Node() {
                Name = HI_ToNodeId(item),
            };

            node.SetAttrGraph("style", "filled")
                .SetAttrGraph("fillcolor", "pink")      // TODO
                .SetAttrGraph("shape", "Mrecord")
                .SetAttrGraph("fontname", "Helvetica")      // Does not have effect at graph level, though it should
                .SetAttrGraph("label", CreateLabel(item));

            return node;
        }

        private static HtmlEntity CreateLabel(HierarchyItem item) {
            HtmlTable table = new HtmlTable()
                .SetAttrHtml("border", 0)
                .SetAttrHtml("cellborder", 0)
                .SetAttrHtml("cellspacing", 0);

            // Header
            string headerText = HtmlUtils.SetFont(HtmlUtils.MakeBold(item.HumanName), 16);

            HtmlTd headerTd = new HtmlTd(headerText)
                .SetAttrHtml("tooltip", CreateNodeToolTip(item))
                .SetAttrHtml("href", item.Graph.SvgUrl);

            table.AddTr(new HtmlTr(headerTd));
            return table;
        }

        private static string CreateNodeToolTip(HierarchyItem item) {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Models: " + HtmlUtils.LINE_BREAK + HtmlUtils.LINE_BREAK);

            foreach (Model model in item.CumulativeModels.OrderBy(x => x.HumanName))
                builder.AppendLine(model.HumanName + HtmlUtils.LINE_BREAK);

            return builder.ToString();
        }
        #endregion

        #region Utils

        private static bool ShowAsSubgraph(HierarchyItem item) {
            return item.Children.Any(x => x.CumulativeModelCount >= Env.MIN_MODELS_TO_SHOW_AS_NODE);
        }

        private static string HI_ToNodeId(HierarchyItem item) {
            string uniqueName = NameUtils.CompoundToSafe(item.CumulativeName);
            if (ShowAsSubgraph(item))
                uniqueName = "cluster_" + uniqueName;
            return uniqueName;
        }

        // When specifying node names in an edge, Graphviz does NOT allow specifying
        // the cluster name. Instead, you must link to a node within the cluster
        // and set the lhead/ltail attributes.
        // https://github.com/glejeune/Ruby-Graphviz/issues/35
        private static string HI_ToNodeIdInEdge(HierarchyItem item) {
            if (ShowAsSubgraph(item))
                return HI_ToNodeIdInEdge(item.Children.First());
            return HI_ToNodeId(item);
        }

        #endregion
    }
}
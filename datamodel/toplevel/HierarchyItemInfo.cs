using System;
using System.Linq;
using System.Collections.Generic;

using datamodel.schema;

namespace datamodel.toplevel {
    // The general intent of this class is to assign metadata to Hierarchy Items.
    // For now, the only piece of metadata we care about is color
    public static class HierarchyItemInfo {

        private static string[] COLORS = new string[] {
            "skyblue", "orchid", "lightsalmon", "#00ff00", "lemonchiffon", "#98fb98", "greenyellow",
            "#ffa500", "deepskyblue", "cyan", "palevioletred", "darkgoldenrod", "#8fbc8f", "honeydew", "#ffff00",
        };

        // Distribute available colors in such a way as to prefer giving colors to items
        // with more models, but do not allow colors on both parent and child
        internal static void AssignColors(HierarchyItem root) {
            // Create a list of all non-root hierarchy items, sorted by model count (descending)
            HashSet<HierarchyItem> all = root.AllDescendents();
            List<HierarchyItem> sorted = all.OrderBy(x => -x.ModelCount).ToList();

            DistributeColors(sorted);
            DropColorToParentIfOnlyChild(sorted);
            PaintModels(sorted);
        }

        // Assign colors in order, and keep iterating to avoid situation where a 
        // parent item has a color and so do its children.
        // Note: algorithm optimized for simplicity, not performance.
        private static void DistributeColors(List<HierarchyItem> sorted) {
            List<string> colors = new List<string>(COLORS);
            HashSet<HierarchyItem> visited = new HashSet<HierarchyItem>();

            while (true) {
                if (colors.Count == 0)
                    break;      // No more colors to hand out

                // Step one: assign colors
                int colorsIndex = 0;
                foreach (HierarchyItem item in sorted) {
                    if (visited.Contains(item)) {
                        // Skip - this is a parent node form which we've removed the color
                    } else if (item.HasColor) {
                        // Skip - already has a color
                    } else {
                        item.ColorString = colors[colorsIndex++];
                        if (colorsIndex >= colors.Count)
                            break;  // No more colors to hand out on this pass
                    }
                }

                // Step two: recycle colors from parent items whose children have colors
                colors.Clear();
                foreach (HierarchyItem item in sorted) {
                    if (item.HasColor && item.AllDescendents().Any(x => x.HasColor)) {
                        visited.Add(item);
                        colors.Add(item.ColorString);
                        item.ColorString = null;
                    }
                }
            }
        }

        // No point having a single colored child - might as well move color to parent
        // At present, this algorithm may not work if colors should be dropped multiple levels
        private static void DropColorToParentIfOnlyChild(List<HierarchyItem> items) {
            foreach (HierarchyItem item in items) {
                if (item.ColorString == null)
                    continue;   // No color to give to parent

                HierarchyItem parent = item.Parent;
                if (parent.IsTop)
                    continue;       // No point coloring the single root level node

                int coloredSiblings = parent.Children.Count(x => x.HasColor);
                if (coloredSiblings == 1) {
                        parent.ColorString = item.ColorString;
                        item.ColorString = null;
                }
            }
        }

        // Paint all models same color as owning HierarchyItem
        private static void PaintModels(List<HierarchyItem> items) {
            foreach (HierarchyItem item in items)
                if (item.ColorString != null)
                    foreach (Model model in item.Models)
                        model.ColorString = item.ColorString;
        }


    }
}
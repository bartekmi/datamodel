using System;
using System.Linq;
using System.Collections.Generic;

using datamodel.schema;

namespace datamodel.toplevel {

    public class HierarchyItem {
        public List<HierarchyItem> Children { get; private set; }
        public HierarchyItem Parent { get; private set; }

        public string Title { get; set; }
        public bool IsUncategorizedCatchall { get; set; }  // E.g. Team items that don't belong to any Engine
        public string ToolTip { get; set; }
        public GraphDefinition Graph { get; set; }

        // List of Models - for Leav Items only
        public Table[] Tables { get; set; }

        public int Level { get; set; }

        // Derived
        public bool IsLeaf { get { return Children.Count == 0; } }
        public bool IsTop { get { return Parent == null; } }
        public bool IsNonLeaf { get { return !IsLeaf; } }
        public string SvgUrl { get { return Graph == null ? null : Graph.SvgUrl; } }
        public bool HasDiagram { get { return SvgUrl != null; } }
        public IEnumerable<Table> CumulativeTables {
            get { return IsLeaf ? Tables : Children.SelectMany(x => x.CumulativeTables); }
        }
        public IEnumerable<string> CumulativeTitle {
            get {
                IEnumerable<string> titleAsEnum = new string[] { Title };
                return IsTop ?
                    titleAsEnum :
                    Parent.CumulativeTitle.Concat(titleAsEnum);
            }
        }
        public int CumulativeModelCount { get { return CumulativeTables.Count(); } }

        private HierarchyItem(HierarchyItem parent) {
            Children = new List<HierarchyItem>();
            Parent = parent;
        }

        public static HierarchyItem CreateHierarchyTree() {
            HierarchyItem topLevel = new HierarchyItem(null) {
                Title = "All Teams",
            };

            foreach (var teamGrup in Schema.Singleton.Tables.GroupBy(x => x.Team).OrderBy(x => x.Key)) {
                HierarchyItem teamItem = new HierarchyItem(topLevel) {
                    Title = teamGrup.Key == null ? "No Team" : ("Team " + teamGrup.Key),
                    IsUncategorizedCatchall = teamGrup.Key == null,
                };
                topLevel.Children.Add(teamItem);

                foreach (var engineGroup in teamGrup.GroupBy(x => x.Engine).OrderBy(x => x.Key)) {

                    HierarchyItem engineItem = new HierarchyItem(teamItem) {
                        Title = engineGroup.Key == null ? "Non-Engine" : ("Engine " + engineGroup.Key),
                        IsUncategorizedCatchall = engineGroup.Key == null,
                    };
                    teamItem.Children.Add(engineItem);

                    foreach (var moduleGroup in engineGroup.GroupBy(x => x.Module).OrderBy(x => x.Key)) {
                        HierarchyItem moduleItem = new HierarchyItem(engineItem) {
                            Title = moduleGroup.Key == null ? "Non-Module" : ("Module " + moduleGroup.Key),
                            IsUncategorizedCatchall = moduleGroup.Key == null,
                            Tables = moduleGroup.ToArray(),
                        };
                        engineItem.Children.Add(moduleItem);

                        // Console.WriteLine(">>>>> {0} > {1} > {2} - {3} Models", teamGrup.Key, engineGroup.Key, moduleGroup.Key, moduleGroup.Count());
                    }
                }
            }

            topLevel.CollapseRedundantChildren();
            return topLevel;
        }

        // A child is "redundant" if...
        // 1) It is an un-categorized placeholder (e.g. no-engine), and
        // 2) It has no other siblings
        private void CollapseRedundantChildren() {

            foreach (HierarchyItem child in Children)
                child.CollapseRedundantChildren();


            if (Children.Count == 1) {                          // See 2) above
                HierarchyItem onlyChild = Children.Single();

                //if (onlyChild.IsUncategorizedCatchall)          // See 1) above
                if (onlyChild.IsLeaf) {
                    Tables = onlyChild.Tables;
                    Children.Clear();
                } else
                    Children = onlyChild.Children;
            }
        }
    }
}
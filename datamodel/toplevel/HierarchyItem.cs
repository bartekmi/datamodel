using System;
using System.Linq;
using System.Collections.Generic;

using datamodel.schema;
using datamodel.metadata;

namespace datamodel.toplevel {

    public class HierarchyItem {
        public List<HierarchyItem> Children { get; private set; }
        public HierarchyItem Parent { get; private set; }

        // The name of the Hierarchy Item - the team, engine, or module
        public string Name { get; private set; }
        public bool IsUncategorizedCatchall { get; set; }  // E.g. Team items that don't belong to any Engine
        public string ToolTip { get; set; }
        public GraphDefinition Graph { get; set; }

        // List of Models - for Leaf Items only
        public Model[] Models { get; set; }

        // Derived
        public bool IsLeaf { get { return Children.Count == 0; } }
        public bool IsTop { get { return Parent == null; } }
        public bool IsNonLeaf { get { return !IsLeaf; } }
        public string SvgUrl { get { return Graph == null ? null : Graph.SvgUrl; } }
        public bool HasDiagram { get { return SvgUrl != null; } }
        public int Level { get { return IsTop ? 0 : Parent.Level + 1; } }
        public IEnumerable<Model> CumulativeModels {
            get { return IsLeaf ? Models : Children.SelectMany(x => x.CumulativeModels); }
        }
        public IEnumerable<string> CumulativeName {
            get {
                IEnumerable<string> asEnumerable = new string[] { Name };
                return IsTop ?
                    asEnumerable :
                    Parent.CumulativeName.Concat(asEnumerable);
            }
        }
        public int CumulativeModelCount { get { return CumulativeModels.Count(); } }

        // This is the only place in the entire codebase that has the knowledge of what the 
        // three levels of the hierachy mean: team, engine, module (in case this changes in the future)
        public string HumanName {
            get {
                switch (Level) {
                    case 0: return "All Models";
                    case 1: return Name == null ? "No Team" : Name + " Team";
                    case 2: return Name == null ? "No Engine" : Name + " Engine";
                    case 3: return Name + " Module";        // If this had no name, it wouldn't exist
                    default:
                        throw new Exception("Unexpected Level: " + Level);
                }
            }
        }

        private HierarchyItem(HierarchyItem parent) {
            Children = new List<HierarchyItem>();
            Parent = parent;
        }

        public override string ToString() {
            return HumanName;
        }

        public static HierarchyItem CreateHierarchyTree() {
            HierarchyItem topLevel = new HierarchyItem(null) {
                Name = "All Teams",
            };

            foreach (var teamGroup in Schema.Singleton.Models.GroupBy(x => x.Team).OrderBy(x => x.Key)) {
                HierarchyItem teamItem = new HierarchyItem(topLevel) {
                    Name = teamGroup.Key,
                    IsUncategorizedCatchall = teamGroup.Key == null,
                };
                topLevel.Children.Add(teamItem);

                foreach (var engineGroup in teamGroup.GroupBy(x => x.Engine).OrderBy(x => x.Key)) {

                    HierarchyItem engineItem = new HierarchyItem(teamItem) {
                        Name = engineGroup.Key,
                        IsUncategorizedCatchall = engineGroup.Key == null,
                    };
                    teamItem.Children.Add(engineItem);

                    foreach (var moduleGroup in engineGroup.GroupBy(x => x.Module).OrderBy(x => x.Key)) {
                        HierarchyItem moduleItem = new HierarchyItem(engineItem) {
                            Name = moduleGroup.Key,
                            IsUncategorizedCatchall = moduleGroup.Key == null,
                            Models = moduleGroup.ToArray(),
                        };
                        engineItem.Children.Add(moduleItem);
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
                    Models = onlyChild.Models;
                    Children.Clear();
                } else
                    Children = onlyChild.Children;
            }
        }
    }
}
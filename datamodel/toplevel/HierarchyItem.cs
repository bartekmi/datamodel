using System;
using System.Linq;
using System.Collections.Generic;

using datamodel.schema;
using datamodel.metadata;
using datamodel.utils;

namespace datamodel.toplevel {

    public class HierarchyItem {
        public List<HierarchyItem> Children { get; private set; }
        public HierarchyItem Parent { get; private set; }

        // The name of the Hierarchy Item - i.e. Level1, Level2, Level3
        public string Name { get; private set; }
        public bool IsUncategorizedCatchall { get; set; }  // E.g. items with no Level1 label
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
        public string UniqueName { get { return string.Join("_", CumulativeName); } }
        public int CumulativeModelCount { get { return CumulativeModels.Count(); } }
        public bool ShouldShowOnIndex { get { return CumulativeModelCount >= Env.MIN_MODELS_TO_SHOW_AS_NODE || Level == 1; } }
        public bool ShouldShowOnIndexAsNode { get { return ShouldShowOnIndex && !Children.Any(x => x.ShouldShowOnIndex); } }

        public string HumanName {
            get {
                Schema schema = Schema.Singleton;

                switch (Level) {
                    case 0: return "All Models";
                    case 1:
                        return Name == null ?
                            string.Format("Orphans (No {0})", schema.GetLevelName(Level - 1)) :
                            Name;
                    case 2:
                        return Name == null ?
                            string.Format("{0} (No {1})", Parent.Name, schema.GetLevelName(Level - 1)) :
                            Name;
                    case 3: return Name;    // If this had no name, it wouldn't exist
                    default:
                        throw new Exception("Unexpected Level: " + Level);
                }
            }
        }
        public string ColorString {
            get {
                HierarchyItem level1_Item = ParentAtLevel(1);
                return Level1Info.GetHtmlColorForLevel1(level1_Item.Name);
            }
        }

        private HierarchyItem(HierarchyItem parent) {
            Children = new List<HierarchyItem>();
            Parent = parent;
        }

        // Note that 'self' is considered a descendent of 'self'
        public bool IsDescendentOf(HierarchyItem other) {
            if (other == this)
                return true;
            return Parent != null && Parent.IsDescendentOf(other);
        }

        public HierarchyItem FindAncestorAtLevel(int level) {
            if (Level == level)
                return this;
            if (Level < level)
                return null;
            return Parent.FindAncestorAtLevel(level);
        }

        public override string ToString() {
            return HumanName;
        }

        // Utility method to apply <action> recursively to all items
        public static void Recurse(HierarchyItem item, Action<HierarchyItem> action) {
            action(item);
            foreach (HierarchyItem child in item.Children)
                Recurse(child, action);
        }

        public static HierarchyItem CreateHierarchyTree() {
            HierarchyItem topLevel = new HierarchyItem(null) {
                Name = "All Models",
            };

            foreach (var l1_Group in Schema.Singleton.Models.GroupBy(x => x.Level1).OrderBy(x => x.Key)) {
                HierarchyItem l1_Item = new HierarchyItem(topLevel) {
                    Name = l1_Group.Key,
                    IsUncategorizedCatchall = l1_Group.Key == null,
                };
                topLevel.Children.Add(l1_Item);

                foreach (var l2_Group in l1_Group.GroupBy(x => x.Level2).OrderBy(x => x.Key)) {

                    HierarchyItem l2_Item = new HierarchyItem(l1_Item) {
                        Name = l2_Group.Key,
                        IsUncategorizedCatchall = l2_Group.Key == null,
                    };
                    l1_Item.Children.Add(l2_Item);

                    foreach (var l3_Group in l2_Group.GroupBy(x => x.Level3).OrderBy(x => x.Key)) {
                        HierarchyItem l3_Item = new HierarchyItem(l2_Item) {
                            Name = l3_Group.Key,
                            IsUncategorizedCatchall = l3_Group.Key == null,
                            Models = l3_Group.ToArray(),
                        };
                        l2_Item.Children.Add(l3_Item);
                    }
                }
            }

            topLevel.AbsorbRedundantChildren();
            return topLevel;
        }

        // We should never have a situation where parent has only one child... The
        // diagrams will be identical and thus redundant.
        private void AbsorbRedundantChildren() {
            while (Children.Count == 1) {
                HierarchyItem onlyChild = Children.Single();

                // "Eat" or "absorb" the child, but keep my name
                Models = onlyChild.Models;
                Children = onlyChild.Children;
            }

            foreach (HierarchyItem child in Children)
                child.AbsorbRedundantChildren();
        }

        public HierarchyItem ParentAtLevel(int level) {
            if (level > Level)
                return null;
            if (Level == level)
                return this;
            return Parent.ParentAtLevel(level);
        }

        public static void DebugPrint(HierarchyItem item, int indent = 0) {
            Console.Write(new string(' ', indent * 2));
            Console.WriteLine(NameUtils.CompoundToSafe(item.CumulativeName));

            foreach (HierarchyItem child in item.Children)
                DebugPrint(child, indent + 1);
        }
    }
}
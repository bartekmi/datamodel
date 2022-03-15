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
        public GraphDefinition Graph { get; set; }
        public string ColorString { get; set; }       // Color associated with this hierarchy item

        // List of Models for this node
        public IEnumerable<Model> Models { get; set; }

        // Derived
        public bool IsLeaf { get { return Children.Count == 0; } }
        public bool IsTop { get { return Parent == null; } }
        public bool IsNonLeaf { get { return !IsLeaf; } }
        public string SvgUrl { get { return Graph == null ? null : Graph.SvgUrl; } }
        public bool HasDiagram { get { return SvgUrl != null; } }
        public int Level { get { return IsTop ? 0 : Parent.Level + 1; } }
        public IEnumerable<string> CumulativeName {
            get {
                IEnumerable<string> asEnumerable = new string[] { Name };
                return IsTop ?
                    asEnumerable :
                    Parent.CumulativeName.Concat(asEnumerable);
            }
        }
        public bool HasColor { get { return ColorString != null; } }
        public string UniqueName { get { return string.Join("_", CumulativeName); } }
        public int ModelCount { get { return Models.Count(); } }
        public bool ShouldShowOnIndex { get { return ModelCount >= Env.MIN_MODELS_TO_SHOW_AS_NODE || Level == 1; } }

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

        // Utility method to apply <action> recursively to all items
        public static void Recurse(HierarchyItem item, Action<HierarchyItem> action) {
            action(item);
            foreach (HierarchyItem child in item.Children)
                Recurse(child, action);
        }

        // Recursively, find all descendents, not including "this"
        public HashSet<HierarchyItem> AllDescendents() {
            HashSet<HierarchyItem> all = new HashSet<HierarchyItem>();
            HierarchyItem.Recurse(this, x => all.Add(x));
            all.Remove(this);
            return all;
        }

        #region Create the Hierarchy
        public static HierarchyItem CreateHierarchyTree() {
            HierarchyItem topLevel = new HierarchyItem(null) {
                Name = "All Models",
                Models = Schema.Singleton.Models,
            };

            CreateHierarchyTreeRecursive(topLevel, Schema.Singleton.Models, 0);

            topLevel.AbsorbRedundantChildren();
            return topLevel;
        }

        private static void CreateHierarchyTreeRecursive(HierarchyItem item, IEnumerable<Model> models, int level) {
            var groups = models.GroupBy(x => x.GetLevel(level)).OrderBy(x => x.Key);

            // There is only a single group and there is no label levels at this level... No point going further.
            if (groups.Count() == 1 && groups.Single().Key == null) {
                foreach (Model model in models)
                    model.LeafHierachyItem = item;
                return;
            }

            foreach (var group in groups) {
                HierarchyItem childItem = new HierarchyItem(item) {
                    Name = group.Key,
                    Models = group,
                };
                item.Children.Add(childItem);

                CreateHierarchyTreeRecursive(childItem, group, level + 1);
            }
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
        #endregion

        public override string ToString() {
            return HumanName;
        }

        public static void DebugPrint(HierarchyItem item, int indent = 0) {
            Console.Write(new string(' ', indent * 2));
            Console.WriteLine(NameUtils.CompoundToSafe(item.CumulativeName));

            foreach (HierarchyItem child in item.Children)
                DebugPrint(child, indent + 1);
        }
    }
}
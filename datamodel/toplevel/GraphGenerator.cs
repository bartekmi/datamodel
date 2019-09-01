using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

using datamodel.schema;
using datamodel.graphviz;
using datamodel.utils;

namespace datamodel.toplevel {

    public static class GraphGenerator {

        internal static void Generate(HierarchyItem item) {
            Recurse(item, CreateGraphDefinition);
            Recurse(item, x => {
                if (x.Graph != null)
                    Generate(x.Graph);
            });
        }

        private static void Recurse(HierarchyItem item, Action<HierarchyItem> action) {
            action(item);
            foreach (HierarchyItem child in item.Children)
                Recurse(child, action);
        }

        private static void CreateGraphDefinition(HierarchyItem item) {
            if (!item.IsTop) {
                item.Graph = new GraphDefinition() {
                    CoreTables = item.CumulativeTables.ToArray(),
                    NameComponents = item.CumulativeTitle.ToArray(),
                };
                UrlService.Singleton.AddGraph(item.Graph);
            }
        }

        internal static void Generate(GraphDefinition graphDef) {

            IEnumerable<Table> allTables = graphDef.CoreTables.Union(graphDef.ExtraTables);
            Dictionary<string, Table> tablesDict = allTables.ToDictionary(x => x.ClassName);

            Dictionary<string, PolymorphicInterface> polymorphicInterfaces = Schema.Singleton.Interfaces.Values
                .Where(x => tablesDict.ContainsKey(x.Table.ClassName))
                .ToDictionary(x => x.Name);

            List<Association> associations = Schema.Singleton.Associations
                .Where(x => tablesDict.ContainsKey(x.OtherSide) &&
                            (tablesDict.ContainsKey(x.FkSide) || x.IsPolymorphic && polymorphicInterfaces.ContainsKey(x.OtherSidePolymorphicName)))
                .ToList();

            (new GraphvizGenerator()).GenerateGraph(
                graphDef,
                graphDef.CoreTables,
                associations,
                graphDef.ExtraTables,
                polymorphicInterfaces.Values.ToList());
        }
    }
}

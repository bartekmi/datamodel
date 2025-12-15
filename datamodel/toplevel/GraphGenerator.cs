using System;
using System.Linq;
using System.Collections.Generic;

using datamodel.schema;
using datamodel.graphviz;
using datamodel.metadata;

namespace datamodel.toplevel {

    public static class GraphGenerator {

        internal static void CreateGraphDefinitions(HierarchyItem top) {
            HierarchyItem.Recurse(top, CreateGraphDefinition);
        }

        private static void CreateGraphDefinition(HierarchyItem item) {
            if (!item.IsTop || Env.GENERATE_TOP_LEVEL_GRAPH) {
                item.Graph = new GraphDefinition() {
                    CoreModels = item.Models.ToArray(),
                    NameComponents = item.CumulativeName.Skip(1).ToArray(),
                    HumanName = item.HumanName,
                    ColorString = item.ColorString,
                };
                UrlService.Singleton.AddGraph(item.Graph);
            }
        }

        internal static void Generate(string outDir, HierarchyItem item, List<GraphDefinition> graphDefsFromMetadata) {
            HierarchyItem.Recurse(item, hierItem => {
                // First, see if a GraphDef was specified explicitly in a visualization.yaml file...
                IEnumerable<string> nameComponents = hierItem.CumulativeName.Skip(1);       // Skip the root node
                GraphDefinition graphDef = graphDefsFromMetadata.SingleOrDefault(gd => gd.HasSameNameAs(nameComponents));

                // If not, just use the generic Graph Definition 
                if (graphDef == null)
                    graphDef = hierItem.Graph;

                if (graphDef != null)
                    Generate(outDir, graphDef);
            });
        }

        internal static void Generate(string outDir, GraphDefinition graphDef) {
            // Extra Models... Models which do not directly appear in this graph, but should be included for the sake of clarity
            IEnumerable<Model> externalSuperclasses = graphDef.CoreModels
                .Where(x => x.Superclass != null)
                .Select(x => x.Superclass)
                .Distinct()
                .Where(x => !graphDef.CoreModels.Contains(x));

            IEnumerable<Model> externalAssociationModels = graphDef.CoreModels
                .SelectMany(x => x.AllAssociations)
                .SelectMany(x => new[] { x.OwnerSideModel, x.OtherSideModel })
                .Distinct()
                .Where(x => x != null)
                .Except(graphDef.CoreModels);

            IEnumerable<Model> externalModels = graphDef.ExtraModels
                .Concat(externalSuperclasses)
                .Concat(externalAssociationModels)
                .Distinct();

            // Calculate Associations to show in the Graph
            IEnumerable<Model> allModels = graphDef.CoreModels.Union(externalModels);
            Dictionary<string, Model> tablesDict = allModels.ToDictionary(x => x.QualifiedName);

            Dictionary<string, PolymorphicInterface> polymorphicInterfaces = Schema.Singleton.Interfaces.Values
                .Where(x => tablesDict.ContainsKey(x.Model.QualifiedName))
                .ToDictionary(x => x.Name);

            List<Association> associations = Schema.Singleton.Associations
                .Where(x => tablesDict.ContainsKey(x.OtherSide) && (tablesDict.ContainsKey(x.OwnerSide)
                    || x.IsPolymorphic && polymorphicInterfaces.ContainsKey(x.PolymorphicName)))
                .ToList();

            // Generate the Graph
            (new GraphvizGenerator(outDir)).GenerateGraph(
                graphDef,
                graphDef.CoreModels,
                associations,
                externalModels,
                polymorphicInterfaces.Values.ToList());
        }
    }
}

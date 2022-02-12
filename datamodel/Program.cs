using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using datamodel.metadata;
using datamodel.schema;
using datamodel.schema.source;
using datamodel.datadict;
using datamodel.toplevel;
using datamodel.utils;
using datamodel.graphviz;

[assembly: InternalsVisibleTo("datamodel_test2")]

namespace datamodel {

    class Program {

        // Once files are generated, use this command to start local web server:
        // If using python3:
        //  python -m http.server 80
        static void Main(string[] args) {
            Env.Configure();

            // Path is relative to 'CWD' attribute in launch.json
            SimpleSource source = new SimpleSource("../datamodel_test2/schema/simple_schema.json");
            Schema.CreateSchema(source);
            Schema schema = Schema.Singleton;

            GenerateGraphsAndDataDictionary();
        }
        
        private static void GenerateGraphsAndDataDictionary() {
            // Extract teams, parse "visualizations.yaml" files, and match paths of Ruby files with models
            List<GraphDefinition> graphDefsFromMetadata = new List<GraphDefinition>();
            ApplyGraphDefsToSchema(graphDefsFromMetadata);

            // Copy static assets to output directory
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"),
                                        Path.Combine(Env.OUTPUT_ROOT_DIR, "assets"));

            HierarchyItem topLevel = HierarchyItem.CreateHierarchyTree();
            GraphGenerator.CreateGraphDefinitions(topLevel);
            GraphGenerator.Generate(topLevel, graphDefsFromMetadata);

            // Since the SVG index is ***embedded*** within the HTML index file,
            // it must be generated first
            GraphvizIndexGenerator.GenerateIndex(topLevel);
            HtmlIndexGenerator.GenerateIndex(Env.OUTPUT_ROOT_DIR, topLevel);

            DataDictionaryGenerator.Generate(Env.OUTPUT_ROOT_DIR, Schema.Singleton.Models);
        }

        private static void ApplyGraphDefsToSchema(List<GraphDefinition> graphDefs) {
            foreach (GraphDefinition graphDef in graphDefs) {
                string[] nameComponents = graphDef.NameComponents;
                if (nameComponents.Length > 1) {
                    // It is expected that if overriding name components are provided at all, they would be:
                    // Team, Engine, [Module]
                    string team = nameComponents[0];
                    string engine = nameComponents[1];
                    string module = nameComponents.Length >= 3 ? nameComponents[2] : null;

                    foreach (Model model in graphDef.CoreModels) {
                        model.Team = team;
                        model.Engine = engine;
                        model.ModuleOverride = module;
                    }
                }
            }
        }
    }
}

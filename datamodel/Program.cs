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

        //********************************************************************************
        // Once files are generated, use this command to start local web server:
        //
        //  python -m http.server 80             # Python 3
        //
        //********************************************************************************

        static void Main(string[] args) {
            Env.Configure();
            Error.Clear();

            var options = new SwaggerSourceOptions() {
                BoringNameComponents = new string[] {
                    "io", "k8s", "api", "pkg", "v1", "v1beta1", "v1beta2", "v2", "v2beta1", "v2beta2"
                },
            };

            //SimpleSource source = new SimpleSource("../datamodel_test2/schema/simple_schema.json");   // Path is relative to 'CWD' attribute in launch.json
            //SwaggerSource source = K8sSwaggerSource.FromFile("../datamodel_test2/schema/swagger_schema.json", options);

            string json = SwaggerSource.DownloadUrl("https://raw.githubusercontent.com/kubernetes/kubernetes/master/api/openapi-spec/swagger.json");
            SwaggerSource source = new K8sSwaggerSource(json, options);

            Schema schema = Schema.CreateSchema(source);
            schema.Level1 = "Level 1";
            schema.Level2 = "Level 2";
            schema.Level3 = "Level 3";

            schema.BoringProperties = new string[] {
                "apiVersion", "kind"
            };

            Level1Info.AssignColor("files", "skyblue");
            Level1Info.AssignColor("run", "lightsalmon");

            GenerateGraphsAndDataDictionary();
        }

        private static void GenerateGraphsAndDataDictionary() {
            // Parse "visualizations.yaml" files
            List<GraphDefinition> graphDefsFromMetadata = new List<GraphDefinition>();
            // TODO: Note that we've temporarily lost this feature - must decide where to keep this.
            ApplyGraphDefsToSchema(graphDefsFromMetadata);

            // Copy static assets to output directory
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"),
                                        Path.Combine(Env.OUTPUT_ROOT_DIR, "assets"));

            // Graphviz MUST have access to images at the exact same path as it must ultimately
            // generate in the svg file. Hence this unsavory solution...
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"), "/assets");

            HierarchyItem topLevel = HierarchyItem.CreateHierarchyTree();
            GraphGenerator.CreateGraphDefinitions(topLevel);
            GraphGenerator.Generate(topLevel, graphDefsFromMetadata);

            // Since the SVG index is ***embedded*** within the HTML index file,
            // it must be generated first
            GraphvizIndexGenerator.GenerateIndex(topLevel);
            HtmlIndexGenerator.GenerateIndex(Env.OUTPUT_ROOT_DIR, topLevel);

            DataDictionaryGenerator.Generate(Env.OUTPUT_ROOT_DIR, Schema.Singleton.Models);
        }

        // TODO: This is currently dead code
        private static void ApplyGraphDefsToSchema(List<GraphDefinition> graphDefs) {
            foreach (GraphDefinition graphDef in graphDefs) {
                string[] nameComponents = graphDef.NameComponents;
                if (nameComponents.Length > 1) {
                    // It is expected that if overriding name components are provided at all, they would be:
                    // Level1, Level2, [Level3]
                    string level1 = nameComponents[0];
                    string level2 = nameComponents[1];
                    string level3 = nameComponents.Length >= 3 ? nameComponents[2] : null;

                    foreach (Model model in graphDef.CoreModels) {
                        model.Level1 = level1;
                        model.Level2 = level2;
                        model.Level3 = level3;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using datamodel.metadata;
using datamodel.schema;
using datamodel.schema.source;
using datamodel.schema.source.from_data;
using datamodel.schema.tweaks;
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

        private static void PrintUsageAndQuit(Dictionary<string, SchemaSource> schemaSources) {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tdotnet run -- <schema-source> <arguments...>");
            Console.WriteLine("Available schema sources:");

            foreach (var item in schemaSources) {
                Console.WriteLine("\t{0} = {1}", item.Key, item.Value.GetType().Name);
            }

            Environment.Exit(1);
        }

        static void Main(string[] args) {
            try {
                Env.Configure();
                Error.Clear();

                Dictionary<string, SchemaSource> schemaSources = new Dictionary<string, SchemaSource>() {
                { "json", new JsonSource() },
                { "yaml", new YamlSource() },
                { "k8s", new K8sSwaggerSource() },
                { "swagger", new SwaggerSource() },
                { "simple", new SimpleSource() },
            };

                if (args.Length < 1)
                    PrintUsageAndQuit(schemaSources);

                string sourceName = args.First();
                if (!schemaSources.TryGetValue(sourceName, out SchemaSource source)) {
                    Console.WriteLine("Unknwon Schema Source: {0}\n", sourceName);
                    PrintUsageAndQuit(schemaSources);
                }

                Parameters parameters = new Parameters(source, args.Skip(1));
                source.Initialize(parameters);
                ApplyGlobalParameters(source, parameters);

                Schema schema = Schema.CreateSchema(source);

                GenerateGraphsAndDataDictionary();
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }

            // JsonSource source = new JsonSource("../datamodel_test2/schema/kubernetes_swagger.json", 
            //     new JsonSource.Options() {
            //         RootObjectName = "kubernetes",
            //         PathsWhereKeyIsData = new string[] {
            //             "properties",
            //         },
            //         SameNameIsSameModel = true,
            //     }
            // );
            // AddKubernetesJsonTweaks(source);

            // YamlSource source = new YamlSource();
            // source.Initialize(new Parameters(source, new string[] { 
            //     @"files=
            //         ../../tmp/f1.yaml,
            //         ../../tmp/f2.yaml,
            //         ../../tmp/f3.yaml,
            //         ../../tmp/f4.yaml,
            //         ../../tmp/f5.yaml",
            //     "title=yaml"
            // }));


            // schema.BoringProperties = new string[] {
            //     "apiVersion", "kind"
            // };

        }

        private static void ApplyGlobalParameters(SchemaSource source, Parameters parameters) {
            string[] tweakJsons = parameters.GetFileContents(Parameters.GLOBAL_PARAM_TWEAKS);
            if (tweakJsons.Length > 0)
                TweakLoader.Load(source, tweakJsons);
        }

        private static void GenerateGraphsAndDataDictionary() {
            // Parse "visualizations.yaml" files
            List<GraphDefinition> graphDefsFromMetadata = new List<GraphDefinition>();

            // Copy static assets to output directory
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"),
                                        Path.Combine(Env.OUTPUT_ROOT_DIR, "assets"));

            // Graphviz MUST have access to images at the exact same path as it must ultimately
            // generate in the svg file. Hence this unsavory solution...
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"), "/assets");

            HierarchyItem topLevel = HierarchyItem.CreateHierarchyTree();
            HierarchyItemInfo.AssignColors(topLevel);
            GraphGenerator.CreateGraphDefinitions(topLevel);
            GraphGenerator.Generate(topLevel, graphDefsFromMetadata);

            // Since the SVG index is ***embedded*** within the HTML index file,
            // it must be generated first
            GraphvizIndexGenerator.GenerateIndex(topLevel);
            HtmlIndexGenerator.GenerateIndex(Env.OUTPUT_ROOT_DIR, topLevel);

            DataDictionaryGenerator.Generate(Env.OUTPUT_ROOT_DIR, Schema.Singleton.Models);
        }
    }
}

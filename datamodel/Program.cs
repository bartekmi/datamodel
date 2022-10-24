using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using datamodel.metadata;
using datamodel.schema;
using datamodel.schema.source;
using datamodel.schema.source.from_data;
using datamodel.schema.source.protobuf;
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
        //  python3 -m http.server 8080
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
                { "k8s", new K8sSwaggerSource() },
                { "proto", new ProtobufSource() },
                { "simple", new SimpleSource() },
                { "swagger", new SwaggerSource() },
                { "yaml", new YamlSource() },
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
                Console.WriteLine(e);
                // Console.WriteLine(BuildMessage(e));
                Environment.Exit(1);
            }
        }

        private static string BuildMessage(Exception e) {
            StringBuilder builder = new StringBuilder();

            bool first = true;
            while (e != null) {
                string line = string.Format("{0}{1}{2}", 
                    first ? "" : "\t", 
                    e.Message, 
                    e.GetType() == typeof(Exception) ? "" : string.Format(" ({0})", e.GetType().Name));

                builder.AppendLine(line);
                e = e.InnerException;
                first = false;
            }

            return builder.ToString();
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
            // Commenting this out for now because...
            // 1. On Ubuntu, we don't generally have free access to the root directory
            // 2. With current version of Graphviz, warnings are generated, but things still work fine
            // DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"), "/assets");

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

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
        private static Parameters _parameters;

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

                Dictionary<string, SchemaSource> schemaSources = new() {
                    { "json", new JsonSource() },
                    { "k8s", new K8sSwaggerSource() },
                    { "proto", new ProtobufSource() },
                    { "simple", new SimpleSource() },
                    { "swagger", new SwaggerSource() },
                    { "yaml", new YamlSource() },
                    { "xsd", new XsdSource() },
                };

                if (args.Length < 1)
                    PrintUsageAndQuit(schemaSources);

                string sourceName = args.First();
                if (!schemaSources.TryGetValue(sourceName, out SchemaSource source)) {
                    Console.WriteLine("Unknwon Schema Source: {0}\n", sourceName);
                    PrintUsageAndQuit(schemaSources);
                }

                _parameters = new Parameters(source, args.Skip(1));
                source.Initialize(_parameters);
         
                string[] tweakJsons = _parameters.GetFileContents(Parameters.GLOBAL_PARAM_TWEAKS);
                if (tweakJsons.Length > 0)
                    TweakLoader.Load(source, tweakJsons);

                // Create Schame
                Schema schema = Schema.CreateSchema(source);
                string dumpSchemaFile = _parameters.GetString(Parameters.GLOBAL_PARAM_DUMP_SCHEMA);
                if (dumpSchemaFile != null) {
                    string schemaDump = JsonUtils.JsonPretty(schema);
                    if (dumpSchemaFile.ToLower() == "true")
                        Console.WriteLine(schemaDump);
                    else
                        File.WriteAllText(dumpSchemaFile, schemaDump);
                }

                // Graph Graphviz output
                if (!_parameters.GetBool(Parameters.GLOBAL_PARAM_NO_GRAPHVIZ))
                    GenerateGraphs();

                // Data Dictionary
                DataDictionaryGenerator.Generate(_parameters.OutDir, Schema.Singleton.Models);
            } catch (Exception e) {
                // The stack trace still provides invaluable debug info, so let's print it.
                Console.WriteLine(e);
                Environment.Exit(1);
            }
        }

        private static void GenerateGraphs() {
            // Parse "visualizations.yaml" files
            List<GraphDefinition> graphDefsFromMetadata = new();

            // Copy static assets to output directory
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"),
                                        Path.Combine(_parameters.OutDir, "assets"));

            HierarchyItem topLevel = HierarchyItem.CreateHierarchyTree();
            HierarchyItemInfo.AssignColors(topLevel);
            GraphGenerator.CreateGraphDefinitions(topLevel);
            GraphGenerator.Generate(_parameters.OutDir, topLevel, graphDefsFromMetadata);

            // Since the SVG index is ***embedded*** within the HTML index file,
            // it must be generated first
            GraphvizIndexGenerator.GenerateIndex(_parameters.OutDir, topLevel);
            HtmlIndexGenerator.GenerateIndex(_parameters.OutDir, topLevel);
        }
    }
}

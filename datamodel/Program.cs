using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

using datamodel.parser;
using datamodel.schema;
using datamodel.tools;
using datamodel.graphviz;
using datamodel.datadict;
using datamodel.graph;
using datamodel.utils;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("datamodel_test2")]

namespace datamodel {

    class Program {

        public static readonly GraphDefinition[] GRAPHS = new GraphDefinition[] {
            // new GraphDefinition("trinity"),
            new GraphDefinition() {
                Engine = "operational_route"
            },
            new GraphDefinition("marketplace"),
            new GraphDefinition("bookings"),
            new GraphDefinition("shipment_data"),
            new GraphDefinition("customs") {
                ExtraTableClassNames = new string[] {"Client", "CompanyEntity", "Shipment"},
                // Len = 2.0,
                Sep = 10.0,
                Style = RenderingStyle.Dot,
            },
        };

        static void Main(string[] args) {
            Env.Configure();

            string command = ExtractArgs(args);

            switch (command) {
                case "genyaml":
                    GenerateYamls();
                    break;
                case "gendocs":
                    ParseModelDirs();        // Extract teams and find paths of Ruby files

                    foreach (Table table in Schema.Singleton.Tables)
                        if (table.ModelPath != null)
                            YamlAnnotationParser.Parse(table);

                    GenerateDataDictionary();
                    CreateGraph();
                    break;
                default:
                    throw new Exception("Unexpected command: " + command);
            }
        }

        private static string ExtractArgs(string[] args) {
            if (args.Length != 1)
                throw new Exception("Exactly one arg expected");

            return args[0];
        }

        private static void GenerateYamls() {
            ParseModelDirs();
            new YamlFileGenerator().Generate(Schema.Singleton, null);
        }

        private static void CreateGraph() {
            Schema schema = Schema.Singleton;
            ParseModelDirs();        // Extract teams and find paths of Ruby files
            GraphGenerator.GenerateAll(GRAPHS);
        }

        private static void GenerateDataDictionary() {
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"),
                                        Path.Combine(Env.OUTPUT_ROOT_DIR, "assets"));
            DataDictionaryGenerator.Generate(Env.OUTPUT_ROOT_DIR, Schema.Singleton.Tables);
        }

        private static void ParseModelDirs() {
            ModelDirParser parser = new ModelDirParser();

            foreach (string dir in Env.MODEL_DIRS)
                parser.ParseDir(Path.Combine(Env.ROOT_MODEL_DIR, dir));
        }
    }
}

// Useful notes I don't want to lose:

// Command line on Windows:
// $ "/c/Program Files (x86)/Graphviz2.38/bin/dot" -Tsvg -obookings_team.svg bookings_team.dot

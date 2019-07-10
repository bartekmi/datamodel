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

[assembly: InternalsVisibleTo("datamodel_test")]

namespace datamodel {

    public static class Env {
        public static string OUTPUT_ROOT_DIR;
        public static string TEMP_DIR;
        public static string REPO_ROOT;
        public static string[] MODEL_DIRS;

        private static void ConfigureWindows() {
            OUTPUT_ROOT_DIR = @"C:\inetpub\wwwroot\datamodel";
            TEMP_DIR = @"C:\TEMP";
            REPO_ROOT = @"C:\datamodel";
            MODEL_DIRS = new string[] { "/datamodel/models", "/datamodel/customs_models" };
        }

        private static void ConfigureMac() {
            OUTPUT_ROOT_DIR = @"~/Sites";
            TEMP_DIR = @"~/temp";
            REPO_ROOT = @"~/datamodel";
            MODEL_DIRS = new string[] { "~/flexport/app/models", "~/flexport/engines/customs/app/models/customs" };
        }

        internal static void Configure() {
            // Obviously add code here to set appropriate env once working on Windows again.
            ConfigureMac();
        }
    }

    class Program {

        public static readonly GraphDefinition[] GRAPHS = new GraphDefinition[] {
            new GraphDefinition("bookings"),
            new GraphDefinition("shipment_data"),
            new GraphDefinition("customs") {
                ExtraTableClassNames = new string[] {"Client", "CompanyEntity", "Shipment"},
                // Len = 2.0,
                Sep = 10.0,
                Style = RenderingStyle.Dot,
            }
        };

        static void Main(string[] args) {
            Env.Configure();

            string command = ExtractArgs(args);

            switch (command) {
                case "genyaml":
                    GenerateYamls();
                    break;
                case "gendocs":
                    GenerateDataDictionary();
                    CreateGraph();
                    break;
                default:
                    throw new Exception("Unexpected command: " + command);
            }
            Error.Clear();

            GenerateDataDictionary();
            CreateGraph();
        }

        private static string ExtractArgs(string[] args) {
            if (args.Length != 1)
                throw new Exception("Exactly one arg expected");

            return args[0];
        }

        private static void GenerateYamls() {
            ParseMainModelDir();
            new YamlFileGenerator().Generate(Schema.Singleton, "bookings");
        }

        private static void CreateGraph() {
            Schema schema = Schema.Singleton;
            ParseMainModelDir();        // Extract teams and find paths of Ruby files

            // This is just a sample to prove that we are reading the human comments
            //Table table = schema.FindByDbName("bookings");
            //List<Error> errors = new List<Error>();
            //YamlAnnotationParser.Parse(table, errors);

            GraphGenerator.GenerateAll(GRAPHS);
        }

        private static void GenerateDataDictionary() {
            Schema schema = Schema.Singleton;
            ParseMainModelDir();        // Extract teams and find paths of Ruby files

            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"),
                                        Path.Combine(Env.OUTPUT_ROOT_DIR, "assets"));
            DataDictionaryGenerator.Generate(Env.OUTPUT_ROOT_DIR, schema.Tables);
        }

        private static void ParseMainModelDir() {
            ModelDirParser parser = new ModelDirParser();

            foreach (string dir in Env.MODEL_DIRS)
                parser.ParseDir(dir);
        }
    }
}

// Useful notes I don't want to lose:

// Command line on Windows:
// $ "/c/Program Files (x86)/Graphviz2.38/bin/dot" -Tsvg -obookings_team.svg bookings_team.dot

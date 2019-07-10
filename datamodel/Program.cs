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
        public static string SCHEMA_FILE;
        public static string GRAPHVIZ_BIN_DIR;
        public static string HTTP_ROOT;

        private static void ConfigureMac() {
            OUTPUT_ROOT_DIR = UserPath(@"Sites");
            TEMP_DIR = UserPath(@"temp");
            REPO_ROOT = UserPath(@"datamodel");
            MODEL_DIRS = new string[] {
                FlexportPath("app/models"),
                FlexportPath("engines/customs/app/models/customs"),
                FlexportPath("engines/operational_route/app/models/operational_route") };
            SCHEMA_FILE = UserPath("datamodel/bartek_raw.txt");
            GRAPHVIZ_BIN_DIR = "/usr/local/bin";
            HTTP_ROOT = "/~bmuszynski";
        }

        private static string FlexportPath(string path) {
            return UserPath(Path.Combine("fcopy", path));
        }

        private static string UserPath(string path) {
            return Path.Combine("/Users/bmuszynski", path);
        }

        #region Windows
        private static void ConfigureWindows() {
            OUTPUT_ROOT_DIR = @"C:\inetpub\wwwroot\datamodel";
            TEMP_DIR = @"C:\TEMP";
            REPO_ROOT = @"C:\datamodel";
            MODEL_DIRS = new string[] { "/datamodel/models", "/datamodel/customs_models" };
            SCHEMA_FILE = @"C:\datamodel\bartek_raw.txt";
            GRAPHVIZ_BIN_DIR = @"C:\Program Files (x86)\Graphviz2.38\bin";
            HTTP_ROOT = "/datamodel";
        }
        #endregion

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

            // This is just a sample to prove that we are reading the human comments
            //Table table = schema.FindByDbName("bookings");
            //List<Error> errors = new List<Error>();
            //YamlAnnotationParser.Parse(table, errors);

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
                parser.ParseDir(dir);
        }
    }
}

// Useful notes I don't want to lose:

// Command line on Windows:
// $ "/c/Program Files (x86)/Graphviz2.38/bin/dot" -Tsvg -obookings_team.svg bookings_team.dot

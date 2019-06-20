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
    // Command line on Windows:
    // $ "/c/Program Files (x86)/Graphviz2.38/bin/dot" -Tsvg -obookings_team.svg bookings_team.dot
    class Program {

        public const string OUTPUT_ROOT_DIR = @"C:\inetpub\wwwroot\datamodel";
        public const string TEMP_DIR = @"C:\TEMP";
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

            Error.Clear();

            GenerateDataDictionary();
            CreateGraph();
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

            DirUtils.CopyDirRecursively(@"C:\datamodel\assets", Path.Combine(OUTPUT_ROOT_DIR, "assets"));
            DataDictionaryGenerator.Generate(OUTPUT_ROOT_DIR, schema.Tables);
        }

        #region Old Tests
        // First few lines of the YAML file for bookings
        // description: What's the diffrence between a booking and a quote request?
        // group: booking_core
        // columns:
        // - name: air
        //     description: Does transportation mode include Air?
        private static void ParseYamlAnnotationFile() {
            Schema schema = Schema.Singleton;
            ParseMainModelDir();        // Extract teams and find paths of Ruby files
            Table table = schema.FindByDbName("bookings");

            List<Error> errors = new List<Error>();
            YamlAnnotationParser.Parse(table, errors);
            AreEqual("What's the diffrence between a booking and a quote request?", table.Description);
            AreEqual("booking_core", table.Group);
            True(table.FindColumn("air").Description.StartsWith("Does transportation mode include Air?"));
        }

        private static void GenerateYamls() {
            ParseMainModelDir();
            new YamlFileGenerator().Generate(Schema.Singleton, "bookings");
        }

        private static void ParseMainModelDir() {
            ModelDirParser parser = new ModelDirParser();
            parser.ParseDir("/datamodel/models");
            parser.ParseDir("/datamodel/customs_models");
        }

        private static void ParseSchema() {
            AreEqual(739, Schema.Singleton.Tables.Count);
        }
        #endregion

        #region Asserts
        private static void AreEqual(object expected, object actual) {
            if (expected == null && actual == null)
                return;
            if (expected == null || !expected.Equals(actual))
                throw new Exception(string.Format("Expected: {0}. Actual: {1}", expected, actual));
        }

        private static void True(bool? value) {
            if (value != true)
                throw new Exception("Value was supposed to be true");
        }

        private static void False(bool? value) {
            if (value != false)
                throw new Exception("Value was supposed to be false");
        }
        #endregion
    }
}

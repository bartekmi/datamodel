using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using datamodel.parser;
using datamodel.schema;
using datamodel.generator;
using datamodel.graphviz;
using datamodel.datadict;

namespace datamodel {
    // Command line on Windows:
    // $ "/c/Program Files (x86)/Graphviz2.38/bin/dot" -Tsvg -obookings_team.svg bookings_team.dot
    class Program {

        public const string DATA_DICTIONARY_DIR = @"/TEMP";
        private const bool DO_ALL_TESTS = false;

        static void Main(string[] args) {

            Error.Clear();

            CreateGraph();
            GenerateDataDictionary();

            if (DO_ALL_TESTS) {
                CreateGraph();
                ParseYamlAnnotationFile();
                GenerateYamls();
                ParseMainModelDir();
                ParseSchema();
            }
        }

        private static void GenerateDataDictionary() {
            Schema schema = Schema.Singleton;
            ParseMainModelDir();        // Extract teams and find paths of Ruby files

            DataDictionaryGenerator.Generate(DATA_DICTIONARY_DIR, schema.Tables.Where(x => x.Team == "bookings"));
        }

        private static void CreateGraph() {
            Schema schema = Schema.Singleton;
            ParseMainModelDir();        // Extract teams and find paths of Ruby files

            Table table = schema.FindByDbName("bookings");
            List<Error> errors = new List<Error>();
            YamlAnnotationParser.Parse(table, errors);

            string output = @"/TEMP/bookings_team.dot";
            (new GraphGenerator()).GenerateGraph(output, schema.Tables.Where(x => x.Team == "bookings"));
        }

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
            string modelsDir = "/datamodel/models";
            new ModelDirParser().ParseDir(modelsDir);
        }

        private static void ParseSchema() {
            AreEqual(739, Schema.Singleton.Tables.Count);
        }

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

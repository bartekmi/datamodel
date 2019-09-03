using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

using datamodel.parser;
using datamodel.schema;
using datamodel.tools;
using datamodel.datadict;
using datamodel.toplevel;
using datamodel.utils;

[assembly: InternalsVisibleTo("datamodel_test2")]

namespace datamodel {

    class Program {

        static void Main(string[] args) {

            Env.Configure();

            string command = ExtractArgs(args);

            switch (command) {
                case "genyaml":
                    GenerateYamls();
                    break;
                case "gendocs":
                    GenerateGraphsAndDataDictionary();
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

        private static void GenerateGraphsAndDataDictionary() {
            ParseModelDirs();        // Extract teams and find paths of Ruby files

            foreach (Model table in Schema.Singleton.Models)
                if (table.ModelPath != null)
                    YamlAnnotationParser.Parse(table);

            // Copy static assets to output directory
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"),
                                        Path.Combine(Env.OUTPUT_ROOT_DIR, "assets"));

            HierarchyItem topLevel = HierarchyItem.CreateHierarchyTree();
            GraphGenerator.Generate(topLevel);

            IndexGenerator.GenerateIndex(Env.OUTPUT_ROOT_DIR, topLevel);
            DataDictionaryGenerator.Generate(Env.OUTPUT_ROOT_DIR, Schema.Singleton.Models);
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

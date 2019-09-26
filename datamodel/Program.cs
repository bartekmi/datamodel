using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

using datamodel.metadata;
using datamodel.schema;
using datamodel.tools;
using datamodel.datadict;
using datamodel.toplevel;
using datamodel.utils;
using datamodel.graphviz;

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
            // Extract teams, parse "visualizations.yaml" files, and match paths of Ruby files with models
            List<GraphDefinition> graphDefsFromMetadata = ParseModelDirs();
            ApplyGraphDefsToSchema(graphDefsFromMetadata);

            foreach (Model table in Schema.Singleton.Models)
                if (table.ModelPath != null)
                    YamlAnnotationParser.Parse(table);

            // Copy static assets to output directory
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"),
                                        Path.Combine(Env.OUTPUT_ROOT_DIR, "assets"));

            HierarchyItem topLevel = HierarchyItem.CreateHierarchyTree();
            GraphGenerator.CreateGraphDefinitions(topLevel);
            // GraphGenerator.Generate(topLevel, graphDefsFromMetadata);

            HtmlIndexGenerator.GenerateIndex(Env.OUTPUT_ROOT_DIR, topLevel);
            GraphvizIndexGenerator.GenerateIndex(topLevel);
            DataDictionaryGenerator.Generate(Env.OUTPUT_ROOT_DIR, Schema.Singleton.Models);
        }

        private static List<GraphDefinition> ParseModelDirs() {
            ModelDirParser parser = new ModelDirParser();
            List<GraphDefinition> graphDefs = new List<GraphDefinition>();

            foreach (string dir in Env.MODEL_DIRS)
                graphDefs.AddRange(parser.ParseRootDir(Path.Combine(Env.ROOT_MODEL_DIR, dir)));

            return graphDefs;
        }

        private static void ApplyGraphDefsToSchema(List<GraphDefinition> graphDefs) {
            foreach (GraphDefinition graphDef in graphDefs) {
                string[] nameComponents = graphDef.NameComponents;
                if (nameComponents.Length > 1) {
                    // It is expected that if overriding name components are provided at all, they would be:
                    // Team, Engine, [Module]
                    string team = nameComponents[0];
                    string engine = nameComponents[1];
                    string module = nameComponents.Length >= 3 ? nameComponents[2] : null;

                    foreach (Model model in graphDef.CoreModels) {
                        model.Team = team;
                        model.Engine = engine;
                        model.ModuleOverride = module;
                    }
                }
            }
        }
    }
}

// Useful notes I don't want to lose:

// Command line on Windows:
// $ "/c/Program Files (x86)/Graphviz2.38/bin/dot" -Tsvg -obookings_team.svg bookings_team.dot

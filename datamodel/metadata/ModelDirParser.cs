using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using datamodel.schema;
using datamodel.utils;

namespace datamodel.metadata {

    // This parser parses (nearly) all Models in our code-base, extracting the following
    // 1) Team Name, 2) Class Name
    public class ModelDirParser {

        public List<GraphDefinition> ParseRootDir(string dirPath) {
            List<GraphDefinition> graphDefs = new List<GraphDefinition>();
            ParseDir(graphDefs, dirPath);
            return graphDefs;
        }

        private void ParseDir(List<GraphDefinition> graphDefs, string dirPath) {
            ParseModelsYamlFile(graphDefs, dirPath);
            ParseFilesInDir(dirPath);

            foreach (string childDirPath in Directory.GetDirectories(dirPath))
                ParseDir(graphDefs, childDirPath);
        }

        private void ParseModelsYamlFile(List<GraphDefinition> graphDefs, string dirPath) {
            if (YamlVisualizationsParser.ExistsInThisDirectory(dirPath)) {
                foreach (GraphDefinition graphDef in YamlVisualizationsParser.Parse(dirPath)) {
                    graphDefs.Add(graphDef);
                }
            }
        }

        private void ParseFilesInDir(string dirPath) {
            int count = 0;

            foreach (string path in Directory.GetFiles(dirPath)) {
                using (StreamReader reader = new StreamReader(path)) {
                    if (!IsActiveRecord(reader, out string className, out string team))
                        continue;

                    Model table = Schema.Singleton.FindByClassName(className);

                    if (table == null) {
                        Error.Log(new Error() {
                            Path = path,
                            Message = string.Format("Model '{0}' not found in schema", className)
                        });
                        continue;
                    }

                    table.ModelPath = path;
                    table.Team = team;

                    if (path.Contains("engine")) {
                        string directory = Path.GetDirectoryName(path);
                        table.Engine = Path.GetFileName(directory);         // Assumes that the last path element of all engines is unique
                    }

                    count++;
                }
            }
        }

        internal static bool IsActiveRecord(TextReader reader, out string className, out string team) {      // internal for testing
            className = null;
            team = null;

            string teamPattern = "#\\s*TEAM:\\s*([a-zA-Z0-9_]+)";
            string modulePattern = "\\s*module\\s+([a-zA-Z0-9_:]+)";
            string classDefPattern = "class\\s+([a-zA-Z0-9_:]+)\\s*<\\s*([a-zA-Z0-9_:]+)";

            string line = null;
            List<string> modules = new List<string>();

            while ((line = reader.ReadLine()) != null) {
                string[] matches = RegExUtils.GetCaptureGroups(line, teamPattern, null);
                if (matches != null)
                    team = matches[0];

                matches = RegExUtils.GetCaptureGroups(line, modulePattern, null);
                if (matches != null)
                    modules.Add(matches[0]);

                matches = RegExUtils.GetCaptureGroups(line, classDefPattern, null);
                if (matches != null) {
                    string baseClass = matches[1];
                    string unqualifiedBaseClass = Model.ExtractUnqualifiedClassName(baseClass);

                    if (unqualifiedBaseClass == "ApplicationRecord" ||
                        // Due to Ruby's complex class scope resolution mechanism, we only require
                        // the un-qualified base class to be among the model classes.
                        // There is an ever-so-slight risk of a non-model class deriving from a base-class
                        // with an identical name to a ligit Model class, but we're ok with that.
                        Schema.Singleton.UnqualifiedClassNameExists(unqualifiedBaseClass)) {

                        className = CreateQualifiedName(modules, matches[0]);
                        return true;
                    }
                }
            }

            return false;
        }

        private static string CreateQualifiedName(IEnumerable<string> modules, string className) {
            List<string> list = new List<string>(modules);
            list.Add(className);
            return string.Join("::", list);
        }
    }
}
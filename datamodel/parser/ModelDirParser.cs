using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using datamodel.schema;
using datamodel.utils;

namespace datamodel.parser {

    public class ModelDirParser {

        private List<Error> _errors;

        public void ParseDir(string dirPath) {
            ParseFilesInDir(dirPath);

            foreach (string childDirPath in Directory.GetDirectories(dirPath))
                ParseDir(childDirPath);
        }

        private void ParseFilesInDir(string dirPath) {
            int count = 0;

            foreach (string path in Directory.GetFiles(dirPath)) {
                using (StreamReader reader = new StreamReader(path)) {
                    if (!IsActiveRecord(reader, out string className, out string team))
                        continue;

                    Table table = Schema.Singleton.FindByClassName(className);

                    if (table == null) {
                        Error.Log(new Error() {
                            Path = path,
                            Message = string.Format("Table '{0}' not found in schema", className)
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

            Console.WriteLine("{0} Models found in {1}", count, dirPath);
        }

        internal static bool IsActiveRecord(TextReader reader, out string className, out string team) {      // internal for testing
            className = null;
            team = null;

            string teamPattern = "#\\s*TEAM:\\s*([a-zA-Z0-9_]+)";
            string modulePattern = "\\s*module\\s*([a-zA-Z0-9_]+)";
            string classDefPattern = "class (([a-zA-Z0-9_]+::)?[a-zA-Z0-9_]+)\\s+<\\s+([a-zA-Z0-9_]+)";

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
                    string baseClass = matches[2];      // match[1] appears to be the inner parentheses before the '::'
                    string qualifiedBaseClass = CreateQualifiedName(modules, baseClass);
                    if (baseClass == "ApplicationRecord" || Schema.Singleton.IsValidClassName(qualifiedBaseClass)) {
                        className = CreateQualifiedName(modules, matches[0]);
                        return true;
                    } else
                        return false;
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
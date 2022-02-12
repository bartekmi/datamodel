using System.IO;
using System.Collections.Generic;

namespace datamodel {
    public static class Env {
        // The root directory where the output SVG diagrams and Data Dictionaries should be placed
        public static string OUTPUT_ROOT_DIR;

        // The path to the temporary files directory. This is used for the intermediate Graphviz files.
        public static string TEMP_DIR;

        // The path to this repository. Used for the source when copying assets (images, css) to the final output
        public static string REPO_ROOT;

        // Directory of the Flexport Repo
        public static string ROOT_MODEL_DIR;

        // List of directory paths where to look for models, relative to ROOT_MODEL_DIR
        public static string[] MODEL_DIRS;

        // The file which contains schema metadata extracted from Rails
        public static string SCHEMA_FILE;

        // Bin directory for Graphviz. It is expected that this contains the programs which generate graphs of different styles, e.g. 'dot'
        public static string GRAPHVIZ_BIN_DIR;

        // The http root where all generated files live relative to the host. This is used when generating hyperlinks. 
        // So all hyperlinks would have the form: <HTTP_ROOT>/relative-url
        // We need this because on the Mac, all user-accessible content lives in http://localhost/~user
        public static string HTTP_ROOT;

        // Generate a graph for the top level of the team hierarchy?
        public static bool GENERATE_TOP_LEVEL_GRAPH;

        // Minimum number of models in the graph index before creating a sub-graph
        public static int MIN_MODELS_TO_SHOW_AS_NODE;

        private static void ConfigureMacTrinity() {
            OUTPUT_ROOT_DIR = UserPath(@"Sites");
            TEMP_DIR = UserPath(@"temp");
            REPO_ROOT = UserPath(@"datamodel");
            ROOT_MODEL_DIR = UserPath("trinity");
            MODEL_DIRS = new string[] {
                "app/models",
            };
            SCHEMA_FILE = UserPath("trinity/bartek_raw_2.txt");
            GRAPHVIZ_BIN_DIR = "/usr/local/bin";
            HTTP_ROOT = "/~bmuszynski";
            GENERATE_TOP_LEVEL_GRAPH = true;
            MIN_MODELS_TO_SHOW_AS_NODE = 1;
        }

        private static void ConfigureMacFlexport() {
            OUTPUT_ROOT_DIR = UserPath(@"Sites");
            TEMP_DIR = UserPath(@"temp");
            REPO_ROOT = UserPath(@"datamodel");
            ROOT_MODEL_DIR = UserPath("flexport");
            MODEL_DIRS = FindModelDirs();
            //MODEL_DIRS = new string[] { "engines/financial_ledger/app/models/financial_ledger/internal" };
            SCHEMA_FILE = UserPath("flexport/bartek_raw_2.txt");
            GRAPHVIZ_BIN_DIR = "/usr/local/bin";
            HTTP_ROOT = "/~bmuszynski";
            GENERATE_TOP_LEVEL_GRAPH = false;
            MIN_MODELS_TO_SHOW_AS_NODE = 20;
        }

        private static string[] FindModelDirs() {
            List<string> modelDirs = new List<string>();

            foreach (string engineFullPath in Directory.GetDirectories(UserPath("flexport/engines"))) {
                string dirFullPath = Path.Combine(engineFullPath, "app", "models");
                if (!Directory.Exists(dirFullPath))
                    continue;
                string dirName = Path.GetFileName(engineFullPath);
                modelDirs.Add(string.Format("engines/{0}/app/models", dirName));
            }

            modelDirs.Add("app/models");
            modelDirs.Add("app/services");      // E.g. Partners::PartnerKnownConsignor. Evil? Who am I to judge.

            return modelDirs.ToArray();
        }

        private static string UserPath(string path) {
            return Path.Combine("/Users/bmuszynski", path);
        }

        #region Windows
        private static void ConfigureWindows() {
            OUTPUT_ROOT_DIR = @"C:\TEMP\datamodel";
            TEMP_DIR = @"C:\TEMP";
            REPO_ROOT = @"C:\github\datamodel";
            ROOT_MODEL_DIR = @"C:\github\datamodel";
            MODEL_DIRS = new string[] { };
            GRAPHVIZ_BIN_DIR = @"C:\Program Files\Graphviz\bin";
            HTTP_ROOT = "";
            GENERATE_TOP_LEVEL_GRAPH = true;
            MIN_MODELS_TO_SHOW_AS_NODE = 1;
        }
        #endregion

        internal static void Configure() {
            // ConfigureMacTrinity();
            // ConfigureMacFlexport();
            ConfigureWindows();
        }
    }
}
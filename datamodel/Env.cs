using System.IO;

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
        }

        private static void ConfigureMac() {
            OUTPUT_ROOT_DIR = UserPath(@"Sites");
            TEMP_DIR = UserPath(@"temp");
            REPO_ROOT = UserPath(@"datamodel");
            ROOT_MODEL_DIR = UserPath("fcopy");
            MODEL_DIRS = new string[] {
                "app/models",
                "app/models/rates",
                "engines/customs/app/models/customs",
                "engines/operational_route/app/models/operational_route" };
            SCHEMA_FILE = UserPath("datamodel/bartek_raw.txt");
            GRAPHVIZ_BIN_DIR = "/usr/local/bin";
            HTTP_ROOT = "/~bmuszynski";
        }

        private static string UserPath(string path) {
            return Path.Combine("/Users/bmuszynski", path);
        }

        #region Windows
        private static void ConfigureWindows() {
            OUTPUT_ROOT_DIR = @"C:\inetpub\wwwroot\datamodel";
            TEMP_DIR = @"C:\TEMP";
            REPO_ROOT = @"C:\datamodel";
            ROOT_MODEL_DIR = @"C:\datamodel";
            MODEL_DIRS = new string[] { "models", "customs_models" };
            SCHEMA_FILE = @"C:\datamodel\bartek_raw.txt";
            GRAPHVIZ_BIN_DIR = @"C:\Program Files (x86)\Graphviz2.38\bin";
            HTTP_ROOT = "/datamodel";
        }
        #endregion

        internal static void Configure() {
            // Obviously add code here to set appropriate env once working on Windows again.
            ConfigureMacTrinity();
        }
    }
}
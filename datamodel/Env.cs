using System.IO;
using System.Collections.Generic;

namespace datamodel {
    public static class Env {
        // The root directory where the output SVG diagrams and Data Dictionaries should be placed
        public static string OUTPUT_ROOT_DIR;

        // The path to this repository. Used for the source when copying assets (images, css) to the final output
        public static string REPO_ROOT;

        // Bin directory for Graphviz. It is expected that this contains the programs which generate graphs of different styles, e.g. 'dot'
        public static string GRAPHVIZ_BIN_DIR;

        // The http root where all generated files live relative to the host. This is used when generating hyperlinks. 
        // So all hyperlinks would have the form: <HTTP_ROOT>/relative-url
        // We need this because on the Mac, all user-accessible content lives in http://localhost/~user
        public static string HTTP_ROOT = "";

        // Generate a graph for the top level of the hierarchy?
        public static bool GENERATE_TOP_LEVEL_GRAPH = true;


        #region Windows
        private static void ConfigureWindows() {
            OUTPUT_ROOT_DIR = @"C:\TEMP\datamodel";
            REPO_ROOT = @"C:\github\datamodel";
            GRAPHVIZ_BIN_DIR = @"C:\Program Files\Graphviz\bin";
        }
        #endregion

        #region Ubuntu
        private static void ConfigureUbuntu() {
            OUTPUT_ROOT_DIR = @"/tmp/datamodel";
            REPO_ROOT = @"..";
            GRAPHVIZ_BIN_DIR = @"/usr/bin";
        }
        #endregion

        #region Mac
        private static void ConfigureMac() {
            OUTPUT_ROOT_DIR = @"/tmp/datamodel";
            REPO_ROOT = @"..";
            GRAPHVIZ_BIN_DIR = @"/opt/homebrew/bin";
        }
        #endregion

        internal static void Configure() {
            ConfigureMac();
        }
    }
}
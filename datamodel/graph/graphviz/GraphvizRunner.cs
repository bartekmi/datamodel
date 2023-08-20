using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using datamodel.metadata;
using datamodel.graphviz.dot;

namespace datamodel.graphviz {
    public static class GraphvizRunner {

        public static void CreateDotAndRun(Graph graph, string outDir, string baseName, RenderingStyle style) {
            string dotPath = Path.Combine(outDir, baseName + ".dot");

            using (TextWriter writer = new StreamWriter(dotPath))
                graph.ToDot(writer);

            string svgPath = Path.Combine(outDir, baseName + ".svg");
            Run(dotPath, svgPath, style);
        }

        public static void Run(string input, string output, RenderingStyle style) {
            string exec = style.ToString().ToLower();
            string path = Path.Combine(Env.GRAPHVIZ_BIN_DIR, exec);

            // We set imagepath for the Graph - this assumes that we are running from project root of datamodel.
            // Without this, dot spits out warnings that it can't find embedded images and strips the images
            // out of the final svg file. Sweath, blood and tears have led me to this revelation.
            // It would be more correct to use the REPO_ROOT Property of Env as that would remove the need 
            // to run from the project root dir.
            string commandLine = string.Format("-Tsvg -o{0} -Gimagepath=.. {1}", output, input);

            if (File.Exists(output))
                File.Delete(output);

            Process process = Process.Start(path, commandLine);
            process.WaitForExit();

            // I used to check just on the exit code, but it looks like there is a bug in GraphViz where it can exit with a bogus
            // error message, yet all seems well. 
            // https://github.com/mdaines/viz.js/issues/134
            if (!File.Exists(output)) {
                Error.Log("{0} {1}", path, commandLine);
                throw new Exception("File not created. Exit Code: " + process.ExitCode);
            }
        }
    }
}

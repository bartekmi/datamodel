using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using datamodel.metadata;
using datamodel.graphviz.dot;

namespace datamodel.graphviz {
    public static class GraphvizRunner {

        public static void Run(string input, string output, RenderingStyle style) {
            string exec = style.ToString().ToLower();
            string path = Path.Combine(Env.GRAPHVIZ_BIN_DIR, exec);
            string commandLine = string.Format("-Tsvg -o{0} {1}", output, input);

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

        public static void CreateDotAndRun(Graph graph, string baseName, RenderingStyle style) {
            string dotPath = Path.Combine(Env.OUTPUT_ROOT_DIR, baseName + ".dot");

            using (TextWriter writer = new StreamWriter(dotPath))
                graph.ToDot(writer);

            string svgPath = Path.Combine(Env.OUTPUT_ROOT_DIR, baseName + ".svg");
            Run(dotPath, svgPath, style);
        }
    }
}

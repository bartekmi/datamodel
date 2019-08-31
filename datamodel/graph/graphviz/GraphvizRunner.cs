using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using datamodel.graph;

namespace datamodel.graphviz {
    public static class GraphvizRunner {

        public static void Run(string input, string output, RenderingStyle style) {
            string exec = style.ToString().ToLower();
            string path = Path.Combine(Env.GRAPHVIZ_BIN_DIR, exec);
            string commandLine = string.Format("-Tsvg -o{0} {1}", output, input);

            Console.WriteLine("About to run...");
            Console.WriteLine("{0} {1}", path, commandLine);

            Process process = Process.Start(path, commandLine);
            process.WaitForExit();

            // TODO: Capture stderr and display

            if (process.ExitCode != 0)
                throw new Exception("Exit Code: " + process.ExitCode);
        }
    }
}

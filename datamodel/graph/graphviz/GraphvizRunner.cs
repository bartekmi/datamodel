using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using datamodel.graph;

namespace datamodel.graphviz {
    public static class GraphvizRunner {

        private const string INSTALL_DIR = @"C:\Program Files (x86)\Graphviz2.38";

        public static void Run(string input, string output, RenderingStyle style) {
            string exec = style.ToString().ToLower();
            string path = Path.Combine(INSTALL_DIR, "bin", exec);
            Process process = Process.Start(path, string.Format("-Tsvg -o{0} {1}", output, input));
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("Exit Code: " + process.ExitCode);
        }
    }
}

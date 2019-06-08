using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace datamodel.graphviz {
    public static class GraphvizRunner {

        private const string INSTALL_DIR = @"C:\Program Files (x86)\Graphviz2.38";

        public static void Run(string input, string output) {
            string exec = Path.Combine(INSTALL_DIR, "bin", "dot");
            Process process = Process.Start(exec, string.Format("-Tsvg -o{0} {1}", output, input));
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("Exit Code: " + process.ExitCode);
        }
    }
}

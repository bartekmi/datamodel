using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel {
    public class Error {

        public string Path { get; set; }
        public string Message { get; set; }
        public int? LineNumber { get; set; }

        public override string ToString() {
            return string.Format("{0}:{1} - {2}", Path, LineNumber, Message);
        }

        public static void Clear() {
            File.Delete(ErrorLog());
        }

        public static void Log(string message, params object[] args) {
            message = string.Format(message, args);
            Log(new Error() { Message = message });
        }

        public static void Log(Error error) {
            using (TextWriter writer = new StreamWriter(ErrorLog(), true))
                writer.WriteLine(error.ToString());
            Console.WriteLine(error);
        }

        private static string ErrorLog() {
            string dir = Env.OUTPUT_ROOT_DIR ?? ".";
            return System.IO.Path.Combine(dir, "datamodel.log");
        }


    }
}
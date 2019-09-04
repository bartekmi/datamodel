using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel {
    public class Error {

        public static string ERROR_LOG = System.IO.Path.Combine(Env.TEMP_DIR, "datamodel.log");

        public string Path { get; set; }
        public string Message { get; set; }
        public int? LineNumber { get; set; }

        public override string ToString() {
            return string.Format("{0}:{1} - {2}", Path, LineNumber, Message);
        }

        public static void Clear() {
            File.Delete(ERROR_LOG);
        }

        public static void Log(string message) {
            Log(new Error() { Message = message });
        }

        public static void Log(Error error) {
            using (TextWriter writer = new StreamWriter(ERROR_LOG, true))
                writer.WriteLine(error.ToString());
            Console.WriteLine(error);
        }
    }
}
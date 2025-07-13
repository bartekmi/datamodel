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
            if (File.Exists(ErrorLog()))
                File.Delete(ErrorLog());
        }

        public static void Log(string message, params object[] args) {
            message = string.Format(message, args);
            Log(new Error() { Message = message });
        }

        public static Action<string> ExtraLogger;
        public static void Log(Error error) {
            if (!Directory.Exists(Env.OUTPUT_LOG_DIR))
                Directory.CreateDirectory(Env.OUTPUT_LOG_DIR);

            using (TextWriter writer = new StreamWriter(ErrorLog(), true))
                writer.WriteLine(error.ToString());
            Console.WriteLine(error);

            ExtraLogger?.Invoke(error.ToString());
        }

        private static string ErrorLog() {
            string dir = Env.OUTPUT_LOG_DIR ?? ".";
            return System.IO.Path.Combine(dir, "datamodel.log");
        }


    }
}
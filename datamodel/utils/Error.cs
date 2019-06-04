using System;
using System.Collections.Generic;
using System.IO;

public class Error {

    public static string ERROR_LOG = "/TEMP/datamodel.log";

    public string Path { get; set; }
    public string Message { get; set; }
    public int? LineNumber { get; set; }

    public override string ToString() {
        return string.Format("{0}:{1} - {2}", Path, LineNumber, Message);
    }

    public static void Clear() {
        File.Delete(ERROR_LOG);
    }

    public static void Append(string message) {
        Append(new Error[] { new Error() { Message = message } });
    }

    public static void Append(IEnumerable<Error> errors) {
        using (TextWriter writer = new StreamWriter(ERROR_LOG, true))
            foreach (Error error in errors)
                writer.WriteLine(error.ToString());
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace datamodel.utils {
    public static class FileUtils {
        public static string SanitizeFilename(string dirtyFilename) {
            char[] invalids = System.IO.Path.GetInvalidFileNameChars();
            string[] pieces = dirtyFilename.Split(invalids, StringSplitOptions.RemoveEmptyEntries);
            string cleanName = String.Join("_", pieces).Replace(' ', '_').TrimEnd('.');
            return cleanName;
        }
    }
}

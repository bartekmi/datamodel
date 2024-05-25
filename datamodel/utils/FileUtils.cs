using System;
using System.Collections.Generic;
using System.Text;

namespace datamodel.utils {
  public static class FileUtils {
    const int MAX_PATH_LENGTH = 100;

    public static string SanitizeFilename(string dirtyFilename) {
      char[] invalids = System.IO.Path.GetInvalidFileNameChars();
      string[] pieces = dirtyFilename.Split(invalids, StringSplitOptions.RemoveEmptyEntries);
      string cleanName = String.Join("_", pieces).Replace(' ', '_').TrimEnd('.');

      // This is a temporary solution dealing with System.IO.PathTooLongException.
      // A better solution would be to make this class non-static, keep a Dictionary mapping
      // between full and truncated paths to avoid truncating two different paths to the
      // same string
      if (cleanName.Length > MAX_PATH_LENGTH) {
        int halfPath = MAX_PATH_LENGTH / 2 - 2;
        cleanName = string.Format("{0}----{1}",
          cleanName.Substring(0, halfPath),
          cleanName.Substring(cleanName.Length - halfPath));
      }
        
      return cleanName;
    }
  }
}

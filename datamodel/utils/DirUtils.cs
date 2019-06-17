using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace datamodel.utils {
    public static class DirUtils {
        public static void CopyDirRecursively(string origin, string destination) {
            DirectoryInfo dir = new DirectoryInfo(origin);

            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            foreach (FileInfo file in dir.GetFiles()) {
                string destPath = Path.Combine(destination, file.Name);
                file.CopyTo(destPath, true);
            }

            foreach (DirectoryInfo childDir in dir.GetDirectories()) {
                string childDirPath = Path.Combine(destination, childDir.Name);
                CopyDirRecursively(childDir.FullName, childDirPath);
            }
        }
    }
}

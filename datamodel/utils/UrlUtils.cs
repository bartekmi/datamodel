using System;
using System.Collections.Generic;
using System.Text;

namespace datamodel.utils {
    public static class UrlUtils {
        public static string ToImageUrl(string imageFile, bool isAbsoluate) {
            string path = "assets/images/" + imageFile;
            return isAbsoluate ? ToAbsolute(path) : path;
        }

        public static string ToCssUrl(string cssFile) {
            return ToAbsolute("assets/css/" + cssFile);
        }

        public static string ToAbsolute(string relativeUrl) {
            relativeUrl = relativeUrl.Replace(' ', '_');            // Urls should match filenames
            if (relativeUrl.StartsWith("/"))
                return Env.HTTP_ROOT + relativeUrl;
            return Env.HTTP_ROOT + "/" + relativeUrl;
        }
    }
}

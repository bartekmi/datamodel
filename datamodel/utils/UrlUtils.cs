using System;
using System.Collections.Generic;
using System.Text;

namespace datamodel.utils {
    public static class UrlUtils {
        public const string HTTP_ROOT = "/datamodel";

        public static string ToImageUrl(string imageFile) {
            return ToAbsolute("assets/images/" + imageFile);
        }

        public static string ToCssUrl(string cssFile) {
            return ToAbsolute("assets/css/" + cssFile);
        }

        public static string ToAbsolute(string relativeUrl) {
            if (relativeUrl.StartsWith("/"))
                return HTTP_ROOT + relativeUrl;
            return HTTP_ROOT + "/" + relativeUrl;
        }
    }
}

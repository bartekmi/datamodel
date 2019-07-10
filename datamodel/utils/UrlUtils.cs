using System;
using System.Collections.Generic;
using System.Text;

namespace datamodel.utils {
    public static class UrlUtils {
        public static string ToImageUrl(string imageFile) {
            return ToAbsolute("assets/images/" + imageFile);
        }

        public static string ToCssUrl(string cssFile) {
            return ToAbsolute("assets/css/" + cssFile);
        }

        public static string ToAbsolute(string relativeUrl) {
            if (relativeUrl.StartsWith("/"))
                return Env.HTTP_ROOT + relativeUrl;
            return Env.HTTP_ROOT + "/" + relativeUrl;
        }
    }
}

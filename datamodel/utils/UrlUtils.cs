using System;
using System.Collections.Generic;
using System.Text;

namespace datamodel.utils {
    public static class UrlUtils {
        public static string ToImageUrl(string imageFile, bool fromNested) {
            return MakeUrl("assets/images/" + imageFile, fromNested);
        }

        public static string ToCssUrl(string cssFile, bool fromNested) {
            return MakeUrl("assets/css/" + cssFile, fromNested);
        }

        public static string MakeUrl(string url, bool fromNested) {
            url = url.Replace(' ', '_');            // Urls should match filenames
            if (fromNested)
                url = "../" + url;
            return url;
        }
    }
}

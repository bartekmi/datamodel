using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public static class HtmlUtils {
        public const string HTTP_ROOT = "/datamodel";

        public static string MakeBold(string text) {
            return string.Format("<b>{0}</b>", text);
        }

        public static string MakeImage(string relativeSource, string url, string imageClass = null) {
            string absSource = ToAbsolute("assets/images/" + relativeSource);
            string classAttr = imageClass == null ? null : string.Format("class='{0}'", imageClass);
            return string.Format("<a href='{0}'><img {1} src='{2}'></a>", url, classAttr, absSource);
        }

        public static string MakeIcon(string relativeSource, string url) {
            return MakeImage(relativeSource, url, "icon");
        }

        public static string ToAbsolute(string relativeUrl) {
            if (relativeUrl.StartsWith("/"))
                return HTTP_ROOT + relativeUrl;
            return HTTP_ROOT + "/" + relativeUrl;
        }
    }
}
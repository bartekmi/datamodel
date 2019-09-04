using datamodel.utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public static class HtmlUtils {

        public const string LINE_BREAK = "&#013;";

        public static string MakeBold(string text) {
            return string.Format("<b>{0}</b>", text);
        }

        public static string MakeImage(string imageName) {
            string imageUrl = UrlUtils.ToImageUrl(imageName);
            return string.Format("<IMG SRC=\"{0}\"/>", imageUrl);
        }

        public static string SetFont(string text, double pointSize) {
            return string.Format("<font point-size=\"{0}\">{1}</font>", pointSize, text);
        }

        public static string Bullet() {
            return "&bull; ";
        }
    }
}
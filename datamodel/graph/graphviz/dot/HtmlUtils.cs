using datamodel.utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public static class HtmlUtils {

        public const string LINE_BREAK = "\n";
        public const string BULLET = "&bull; ";
        public const string ASTERISK = "*";

        public static string MakeBold(string text) {
            return string.Format("<b>{0}</b>", text);
        }

        public static string MakeImage(string imageName) {
            string imageUrl = UrlUtils.ToImageUrl(imageName, true);
            return string.Format("<IMG SRC=\"{0}\"/>", imageUrl);
        }

        public static string SetFont(string text, double pointSize) {
            return string.Format("<font point-size=\"{0}\">{1}</font>", pointSize, text);
        }
    }
}
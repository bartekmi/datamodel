using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public static class HtmlUtils {
        public static string MakeBold(string text) {
            return string.Format("<b>{0}</b>", text);
        }

        public static string SetFont(string text, double pointSize) {
            return string.Format("<font point-size=\"{0}\">{1}</font>", pointSize, text);
        }

        public static string Bullet() {
            return "&bull; ";
        }
    }
}
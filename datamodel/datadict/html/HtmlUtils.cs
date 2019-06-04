using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public static class HtmlUtils {
        public static string MakeBold(string text) {
            return string.Format("<b>{0}</b>", text);
        }
    }
}
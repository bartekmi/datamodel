using System;
using System.Collections.Generic;
using System.IO;

using datamodel.utils;

namespace datamodel.datadict.html {
    public static class HtmlUtils {
        public static string MakeBold(string text) {
            return string.Format("<b>{0}</b>", text);
        }

        public static string MakeImage(string imageName, string url, string imageClass = null, string toolTip = null) {
            string absSource = UrlUtils.ToImageUrl(imageName);
            string classAttr = imageClass == null ? null : string.Format("class='{0}'", imageClass);
            string titleAttr = toolTip == null ? null : string.Format("title='{0}'", toolTip);
            return string.Format("<a href='{0}'><img {1} {2} src='{3}'></a>", url, classAttr, titleAttr, absSource);
        }

        public static string MakeIcon(string relativeSource, string url, string toolTip, string cssClass = "icon") {
            return MakeImage(relativeSource, url, cssClass, toolTip);
        }
    }
}
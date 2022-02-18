using System;
using System.Collections.Generic;
using System.Text;

using datamodel.utils;
using datamodel.schema;
using datamodel.toplevel;
using datamodel.metadata;

namespace datamodel.datadict.html {
    public static class HtmlUtils {

        public const string LINE_BREAK = "\n";

        public static string MakeBold(string text) {
            return string.Format("<b>{0}</b>", text);
        }

        public static HtmlRaw MakeIcon(string relativeSource, string url, string toolTip, string cssClass = "icon") {
            return MakeImage(relativeSource, url, cssClass, toolTip);
        }

        public static HtmlRaw MakeIconForDocs(Model model) {
            string docToolTip = string.Format("Go to Data Dictionary of linked table: {0}", model.HumanName);
            return MakeIcon(IconUtils.DOCS, UrlService.Singleton.DocUrl(model), docToolTip);
        }

        public static HtmlBase MakeIconsForDiagrams(Model model, string cssClass) {
            List<GraphDefinition> graphs = UrlService.Singleton.GetGraphs(model);
            HtmlElement span = new HtmlElement("span");

            foreach (GraphDefinition graph in graphs)
                span.Add(MakeIconForDiagram(graph, cssClass));

            return span;
        }

        public static HtmlBase MakeIconForDiagram(GraphDefinition graph, string cssClass) {
            string text = string.Format("{0} ({1})", graph.HumanName, graph.CoreModels.Length);
            string toolTip = string.Format("Go to diagram which contains this Model...{0}Title: {1}{0}Number of Models: {2}",
                LINE_BREAK, graph.HumanName, graph.CoreModels.Length);

            HtmlBase link = MakeLink(graph.SvgUrl, text, cssClass, toolTip);

            return link;
        }


        public static HtmlRaw MakeImage(string imageName, string url, string imageClass = null, string toolTip = null) {
            string absSource = UrlUtils.ToImageUrl(imageName);
            string classAttr = imageClass == null ? null : string.Format("class='{0}'", imageClass);
            string titleAttr = toolTip == null ? null : string.Format("title='{0}'", toolTip);
            string rawHtml = string.Format("<a href='{0}'><img {1} {2} src='{3}'></a>", url, classAttr, titleAttr, absSource);
            return new HtmlRaw(rawHtml);
        }

        public static HtmlRaw MakeLink(string url, string text, string cssClass = null, string toolTip = null) {
            string classAttr = cssClass == null ? null : string.Format("class='{0}'", cssClass);
            string titleAttr = toolTip == null ? null : string.Format("title='{0}'", toolTip);
            string rawHtml = string.Format("<a href='{0}' {1} {2}>{3}</a>", url, classAttr, titleAttr, text);
            return new HtmlRaw(rawHtml);
        }

        public static HtmlElement CreatePage(out HtmlElement body) {
            HtmlElement html = new HtmlElement("html");
            html.Add(new HtmlElement("head")
                    .Add(new HtmlElement("link")
                        .Attr("rel", "stylesheet")
                        .Attr("href", UrlUtils.ToCssUrl("datadict.css"))));

            body = html.Add(new HtmlElement("body"));
            return html;
        }

        private static Dictionary<char, string> _charToEscape = new Dictionary<char, string>() {
            { '\n', "<br>" },
        };

        public static string Sanitize(string text) {
            if (text == null)
                return null;

            StringBuilder builder = new StringBuilder();

            foreach (char c in text) {
                if (_charToEscape.TryGetValue(c, out string replacement))
                    builder.Append(replacement);
                else    
                    builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
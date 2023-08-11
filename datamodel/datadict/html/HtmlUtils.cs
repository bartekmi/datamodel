using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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

        public static HtmlRaw MakeIcon(string relativeSource, string url, string toolTip, bool fromNested) {
            return MakeImage(relativeSource, url, "icon", toolTip, fromNested);
        }

        public static HtmlRaw MakeIconForDocs(Model model, bool fromNested) {
            string docToolTip = string.Format("Go to Data Dictionary of linked table: {0}", model.HumanName);
            return MakeIcon(IconUtils.DOCS, UrlService.Singleton.DocUrl(model, fromNested), docToolTip, fromNested);
        }

        public static HtmlBase MakeIconsForDiagrams(Model model, string cssClass, bool fromNested) {
            List<GraphDefinition> graphs = UrlService.Singleton.GetGraphs(model);
            HtmlElement span = new("span");

            foreach (GraphDefinition graph in graphs)
                span.Add(MakeIconForDiagram(graph, cssClass, fromNested));

            return span;
        }

        private static HtmlBase MakeIconForDiagram(GraphDefinition graph, string cssClass, bool fromNested) {
            string text = string.Format("{0} ({1})", graph.HumanName, graph.CoreModels.Length);
            string toolTip = string.Format("Go to diagram which contains this Model...{0}Title: {1}{0}Number of Models: {2}",
                LINE_BREAK, graph.HumanName, graph.CoreModels.Length);

            HtmlBase link = MakeLink(graph.GetSvgUrl(fromNested), text, cssClass, toolTip, graph.ColorString);

            return link;
        }


        public static HtmlRaw MakeImage(string imageName, string url, string cssClass, string toolTip, bool fromNested) {
            string absSource = UrlUtils.ToImageUrl(imageName, fromNested);
            string classAttr = cssClass == null ? null : string.Format("class='{0}'", cssClass);
            string titleAttr = toolTip == null ? null : string.Format("title='{0}'", Sanitize(toolTip, true));
            string rawHtml = string.Format("<img {0} {1} src='{2}'>", classAttr, titleAttr, absSource);

            if (url != null)
                rawHtml = string.Format("<a href='{0}'>{1}</a>", url, rawHtml);

            return new HtmlRaw(rawHtml);
        }

        public static HtmlRaw MakeLink(string url, string text, string cssClass = null, string toolTip = null, string color = null) {
            string classAttr = cssClass == null ? null : string.Format("class='{0}'", cssClass);
            string toolTipAttr = toolTip == null ? null : string.Format("title='{0}'", Sanitize(toolTip, true));
            string backgroundAttr = color == null ? null : string.Format("style='background:{0}'", color);
            string rawHtml = string.Format("<a href='{0}' {1} {2} {3}>{4}</a>", url, classAttr, toolTipAttr, backgroundAttr, text);
            return new HtmlRaw(rawHtml);
        }

        public static HtmlElement CreatePage(out HtmlElement body, bool fromNested) {
            HtmlElement html = new("html");
            html.Add(new HtmlElement("head")
                    .Add(new HtmlElement("link")
                        .Attr("rel", "stylesheet")
                        .Attr("href", UrlUtils.ToCssUrl("datadict.css", fromNested))));

            body = html.Add(new HtmlElement("body"));
            return html;
        }

        // https://stackoverflow.com/questions/7381974/which-characters-need-to-be-escaped-in-html
        private static readonly Dictionary<char, string> _charToEscape = new() {
            { '"', "&quot;" },
            { '&', "&amp;" },
            { '<', "&lt;" },
            { '>', "&gt;" },

            { '\'', "&#39;" },
            { '|', "&#124;" },
        };

        public static string Sanitize(string text, bool inToolTip) {
            if (text == null)
                return null;

            StringBuilder builder = new();

            foreach (char c in text) {
                if (_charToEscape.TryGetValue(c, out string replacement))
                    builder.Append(replacement);
                else if (c == '\n') 
                    // https://www.askingbox.com/question/html-line-break-in-the-title-attribute-or-tooltip
                    builder.Append(inToolTip ? "&#10;" : "<br>");
                else    
                    builder.Append(c);
            }

            string sanitized = builder.ToString();
            string sanitizedWithLinks = AddHrefLinks(sanitized);

            return sanitizedWithLinks;
        }

        // Convert text which may have http(s) links by adding <a> tags to make
        // the links functional
        // https://stackoverflow.com/questions/32637/easiest-way-to-convert-a-url-to-a-hyperlink-in-a-c-sharp-string/32693
        private static string AddHrefLinks(string text) {
            Regex r = new(@"(https?://[^\s]+)");
            text = r.Replace(text, "<a href=\"$1\">$1</a>");
            return text;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using datamodel.utils;
using datamodel.toplevel;
using datamodel.datadict.html;
using datamodel.schema;

namespace datamodel.datadict {
    public static class HtmlIndexGenerator {

        #region Top Level
        public static void GenerateIndex(string rootDir, HierarchyItem topLevel) {

            HtmlElement html = HtmlUtils.CreatePage(out HtmlElement body, false);
            HtmlElement topDiv = CreateStyledDiv("index-top-level");
            body.Add(topDiv);

            // Title/Header
            topDiv.Add(new HtmlElement("h1", Schema.Singleton.Title));

            // Picture Index
            // I originally tried using the HTML <object> tag for this but there were several issues
            // around this... the navigation would behave as if the SVG was in a frame
            string svgIndexPath = Path.Combine(rootDir, "index.svg");
            topDiv.Add(new HtmlInclude(svgIndexPath));

            // Text Index
            HtmlElement sideBySideDiv = CreateStyledDiv("index-side-by-side");
            body.Add(sideBySideDiv);
            sideBySideDiv.Add(GenerateHierarchy(topLevel));
            sideBySideDiv.Add(GenerateFlatList());

            string path = Path.Combine(rootDir, "index.html");
            using StreamWriter writer = new(path);
            html.ToHtml(writer, 0);
        }

        private static HtmlElement CreateStyledDiv(string cssClass) {
            HtmlElement topContainer = new HtmlElement("div")
                .Class(cssClass);
            return topContainer;
        }
        #endregion

        #region Hierarchy
        private static HtmlElement GenerateHierarchy(HierarchyItem hierarchyItem) {
            HtmlElement hierarchyHtml = new HtmlElement("div").Class("index-subpanel");
            hierarchyHtml.Add(new HtmlElement("h2", "Hierarchical Index"));
            AddHierarchyToParentRecursively(hierarchyHtml, hierarchyItem);
            return hierarchyHtml;
        }
        private static void AddHierarchyToParentRecursively(HtmlElement parent, HierarchyItem itemHier) {
            HtmlElement item = AddHierarchyToParent(parent, itemHier);
            if (itemHier.IsNonLeaf)
                AddChildrenToList(item, itemHier);
        }

        private static HtmlElement AddHierarchyToParent(HtmlElement parent, HierarchyItem hierItem) {
            HtmlElement htmlItem = new("li");
            parent.Add(htmlItem);

            string text = string.Format("{0} ({1} Models)", hierItem.HumanName, hierItem.ModelCount);

            if (hierItem.HasDiagram) 
                htmlItem.Add(HtmlUtils.MakeLink(hierItem.GetSvgUrl(false), text, null, null, hierItem.ColorString));
            else
                htmlItem.Text = text;

            return htmlItem;
        }

        private static void AddChildrenToList(HtmlElement list, HierarchyItem itemHier) {
            HtmlElement ul = new("ul");
            list.Add(ul);

            foreach (HierarchyItem child in itemHier.Children)
                AddHierarchyToParentRecursively(ul, child);
        }

        #endregion

        #region Flat List
        private static HtmlElement GenerateFlatList() {
            HtmlElement list = new HtmlElement("div").Class("index-subpanel");
            list.Add(new HtmlElement("h2", "All Models, Alphabetical"));

            foreach (Model model in Schema.Singleton.Models.OrderBy(x => x.HumanName))
                list.Add(GenerateFlatListItem(model));

            return list;
        }

        private static HtmlElement GenerateFlatListItem(Model model) {
            HtmlElement div = new("div");
            HtmlElement span = div.Add(new HtmlElement("span"));

            span.Add(HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(model, false), model.HumanName, null, model.Description));
            span.Add(HtmlUtils.MakeIconForDocs(model, false));
            span.Add(HtmlUtils.MakeIconsForDiagrams(model, "text-icon", false));

            return div;
        }
        #endregion
    }
}
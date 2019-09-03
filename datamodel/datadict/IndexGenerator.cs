using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using datamodel.utils;
using datamodel.toplevel;
using datamodel.datadict.html;
using datamodel.schema;

namespace datamodel.datadict {
    public static class IndexGenerator {

        #region Top Level
        public static void GenerateIndex(string rootDir, HierarchyItem topLevel) {

            HtmlElement html = HtmlUtils.CreatePage(out HtmlElement body);
            HtmlElement topContainer = GenerateTopContainer();
            body.Add(topContainer);

            topContainer.Add(GenerateHierarchy(topLevel));
            topContainer.Add(GenerateFlatList());

            string path = Path.Combine(rootDir, "index.html");
            using (StreamWriter writer = new StreamWriter(path))
                html.ToHtml(writer, 0);
        }

        private static HtmlElement GenerateTopContainer() {
            HtmlElement topContainer = new HtmlElement("div")
                .Class("index-container");
            return topContainer;
        }
        #endregion

        #region Hierarchy
        private static HtmlElement GenerateHierarchy(HierarchyItem hierarchyItem) {
            HtmlElement hierarchyHtml = new HtmlElement("div").Class("index-subpanel");
            AddHierarchyToParentRecursively(hierarchyHtml, hierarchyItem);
            return hierarchyHtml;
        }
        private static void AddHierarchyToParentRecursively(HtmlElement parent, HierarchyItem itemHier) {
            HtmlElement item = AddHierarchyToParent(parent, itemHier);
            if (itemHier.IsNonLeaf)
                AddChildrenToList(item, itemHier);
        }

        private static HtmlElement AddHierarchyToParent(HtmlElement parent, HierarchyItem itemHier) {
            HtmlElement itemHtml = new HtmlElement("li");
            parent.Add(itemHtml);

            if (itemHier.HasDiagram) {
                HtmlElement span = new HtmlElement("span");
                span.Add(HtmlUtils.MakeLink(itemHier.SvgUrl, itemHier.Title, itemHier.ToolTip));
                span.Add(HtmlUtils.MakeIconForDiagram(itemHier.Graph, "text-icon"));
                itemHtml.Add(span);
            } else {
                itemHtml.Text = string.Format("{0} ({1} Models)", itemHier.Title, itemHier.CumulativeModelCount);
            }

            return itemHtml;
        }

        private static void AddChildrenToList(HtmlElement list, HierarchyItem itemHier) {
            HtmlElement ul = new HtmlElement("ul");
            list.Add(ul);

            foreach (HierarchyItem child in itemHier.Children)
                AddHierarchyToParentRecursively(ul, child);
        }

        #endregion

        #region Flat List
        private static HtmlElement GenerateFlatList() {
            HtmlElement list = new HtmlElement("div").Class("index-subpanel");

            foreach (Model table in Schema.Singleton.Models.OrderBy(x => x.HumanName))
                list.Add(GenerateFlatListItem(table));

            return list;
        }

        private static HtmlElement GenerateFlatListItem(Model table) {
            HtmlElement div = new HtmlElement("div");
            HtmlElement span = div.Add(new HtmlElement("span"));

            span.Add(HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(table), table.HumanName, table.Description));
            span.Add(HtmlUtils.MakeIconForDocs(table));
            span.Add(HtmlUtils.MakeIconsForDiagrams(table, "text-icon"));

            return div;
        }
        #endregion
    }
}
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

            string text = string.Format("{0} ({1} Models)", itemHier.HumanName, itemHier.CumulativeModelCount);

            if (itemHier.HasDiagram)
                itemHtml.Add(HtmlUtils.MakeLink(itemHier.SvgUrl, text, itemHier.ToolTip));
            else
                itemHtml.Text = text;

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

            foreach (Model model in Schema.Singleton.Models.OrderBy(x => x.HumanName))
                list.Add(GenerateFlatListItem(model));

            return list;
        }

        private static HtmlElement GenerateFlatListItem(Model model) {
            HtmlElement div = new HtmlElement("div");
            HtmlElement span = div.Add(new HtmlElement("span"));

            span.Add(HtmlUtils.MakeLink(UrlService.Singleton.DocUrl(model), model.HumanName, model.Description));
            span.Add(HtmlUtils.MakeIconForDocs(model));
            span.Add(HtmlUtils.MakeIconsForDiagrams(model, "text-icon"));

            return div;
        }
        #endregion
    }
}
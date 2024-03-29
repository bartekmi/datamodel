using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace datamodel.graphviz.dot {
    public class HtmlTr : HtmlEntity {
        private List<HtmlTd> _tds = new List<HtmlTd>();

        // Create a row with single Td
        public HtmlTr(string htmlText) {
            AddTd(new HtmlTd(htmlText));
        }

        public HtmlTr(params HtmlTd[] tds) {
            foreach (HtmlTd td in tds)
                AddTd(td);
        }

        public void AddTd(HtmlTd td) {
            _tds.Add(td);
        }

        public void SetAttrAllChildren(string name, object value) {
            foreach (HtmlTd td in _tds)
                td.SetAttrHtml(name, value);
        }

        override public void ToHtml(TextWriter writer) {
            WriteOpeningTag(writer, "tr");
            foreach (HtmlTd td in _tds)
                td.ToHtml(writer);
            WriteClosingTag(writer, "tr");
        }
    }
}
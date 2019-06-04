using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlTable : HtmlEntity {
        private List<HtmlTr> _trs = new List<HtmlTr>();

        public void AddTr(HtmlTr tr) {
            _trs.Add(tr);
        }

        override public void ToHtml(TextWriter writer) {
            WriteOpeningTag(writer, "table");
            foreach (HtmlTr tr in _trs) {
                tr.ToHtml(writer);
                writer.WriteLine();
            }
            WriteClosingTag(writer, "table");
        }

    }
}
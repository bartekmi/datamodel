using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlTr : HtmlElement {

        public HtmlTr() : base("tr") { }

        // Create a row with single Td
        public HtmlTr(string htmlText) : this(new HtmlTd(htmlText)) {
            // Do nothing
        }

        public HtmlTr(params HtmlTd[] tds) : base("tr") {
            foreach (HtmlTd td in tds)
                Add(td);
        }

        public HtmlTr(params HtmlTh[] tds) : base("tr") {
            foreach (HtmlTh td in tds)
                Add(td);
        }
    }
}
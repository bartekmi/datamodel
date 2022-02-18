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

        public HtmlTr(params HtmlTd[] tds) : this() {
            foreach (HtmlTd td in tds)
                Add(td);
        }

        public HtmlTr(params HtmlTh[] ths) : this() {
            foreach (HtmlTh td in ths)
                Add(td);
        }

        public HtmlTr(params string[] datas) : this() {
            foreach (string data in datas)
                Add(new HtmlTd(data));
        }
    }
}
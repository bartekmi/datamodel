using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlTable : HtmlElement {

        public HtmlTable() : base("table") { }

        public void AddTr(HtmlTr tr) {
            Add(tr);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlTh : HtmlElement {
        public HtmlTh() : this(null) {}
        public HtmlTh(string text) : base("th", text) { }
    }
}
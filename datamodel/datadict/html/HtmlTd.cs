using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlTd : HtmlElement {
        public HtmlTd(string text, bool doNotEscape = false) : base("td", text, doNotEscape) { }
        public HtmlTd(params HtmlBase[] children) : base("td", children) { }
    }
}
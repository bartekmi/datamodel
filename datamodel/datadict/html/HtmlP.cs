using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlP : HtmlElement {
        public HtmlP(string text) : base("p", text) { }
    }
}
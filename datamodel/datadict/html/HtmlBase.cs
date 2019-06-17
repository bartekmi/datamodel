using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace datamodel.datadict.html {
    public abstract class HtmlBase {
        public abstract void ToHtml(TextWriter writer);
    }
}

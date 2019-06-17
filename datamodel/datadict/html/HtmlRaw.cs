using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace datamodel.datadict.html {
    public class HtmlRaw : HtmlBase {
        public string Text { get; private set; }

        public HtmlRaw(string text) {
            Text = text;
        }

        public override void ToHtml(TextWriter writer) {
            writer.Write(Text);
        }
    }
}

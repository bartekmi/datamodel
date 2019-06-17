using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public class HtmlTd : HtmlEntity {

        public string Text;

        public HtmlTd(string text = null) {
            Text = text;
        }

        override public void ToHtml(TextWriter writer) {
            WriteOpeningTag(writer, "td");
            writer.Write(Text);
            WriteClosingTag(writer, "td");
        }
    }
}
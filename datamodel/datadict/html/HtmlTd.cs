using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlTd : HtmlEntity {

        private string _text;

        public HtmlTd(string text) {
            _text = text;
        }

        override public void ToHtml(TextWriter writer) {
            WriteOpeningTag(writer, "td");
            writer.Write(_text);
            WriteClosingTag(writer, "td");
        }
    }
}
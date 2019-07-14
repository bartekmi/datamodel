using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace datamodel.datadict.html {
    public abstract class HtmlBase {
        private int INDENT_STEP = 2;

        public abstract void ToHtml(TextWriter writer, int indent);

        protected void WriteIndent(TextWriter writer, int indent) {
            writer.Write(new string(' ', indent * INDENT_STEP));
        }

    }
}

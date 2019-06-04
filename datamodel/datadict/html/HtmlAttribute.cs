using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlAttribute {
        public string Name { get; set; }
        public object Value { get; set; }

        public HtmlAttribute(string name, object value) {
            Name = name;
            Value = value;
        }

        public void Render(TextWriter writer) {
            writer.Write(string.Format("{0}=\"{1}\"", Name, Value));
        }
    }
}
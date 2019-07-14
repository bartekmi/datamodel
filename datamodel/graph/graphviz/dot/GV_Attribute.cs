using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public class GV_Attribute {
        public string Name { get; set; }
        public object Value { get; set; }

        public GV_Attribute(string name, object value) {
            Name = name;
            Value = value;
        }

        public void ToDot(TextWriter writer) {
            if (Value is HtmlEntity)
                writer.Write(string.Format("{0}=<{1}>", Name, (Value as HtmlEntity).ToHtml()));
            else
                writer.Write(string.Format("{0}=\"{1}\"", Name, SanitizeValue(Value)));
        }

        private string SanitizeValue(object value) {
            if (value == null)
                return null;
            return value.ToString().Replace('"', '\'');
        }
    }
}
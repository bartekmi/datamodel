using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        // https://mateam.net/html-escape-characters/
        private static Dictionary<char, string> _charToEscape = new Dictionary<char, string>() {
            { '"', "\'" },
            { '&', "&amp;" },
            { '<', "&lt;" },
            { '>', "&gt;" },
            
            { '\n', "&#13;" },
            { '{', "&#123;" },
            { '|', "&#124;" },
            { '}', "&#125;" },
        };

        private string SanitizeValue(object value) {
            if (value == null)
                return null;

            StringBuilder builder = new StringBuilder();

            foreach (char c in value.ToString()) {
                if (_charToEscape.TryGetValue(c, out string replacement))
                    builder.Append(replacement);
                else    
                    builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
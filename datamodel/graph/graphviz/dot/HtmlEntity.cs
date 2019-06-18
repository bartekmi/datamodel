using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public abstract class HtmlEntity {
        private List<GV_Attribute> _attributes = new List<GV_Attribute>();

        public abstract void ToHtml(TextWriter writer);

        public string ToHtml() {
            using (StringWriter writer = new StringWriter()) {
                ToHtml(writer);
                return writer.ToString();
            }
        }

        internal void SetAttributeInternal(string name, object value) {
            _attributes.Add(new GV_Attribute(name, value));
        }

        protected void WriteOpeningTag(TextWriter writer, string tag) {
            writer.Write("<" + tag);
            foreach (GV_Attribute attribute in _attributes) {
                if (attribute.Value != null) {
                    writer.Write(" ");
                    attribute.ToDot(writer);
                }
            }
            writer.Write(">");
        }

        protected void WriteClosingTag(TextWriter writer, string tag) {
            writer.Write(string.Format("</{0}>", tag));
        }
    }

    public static class HtmlEntityExtensions {
        public static T SetAttrHtml<T>(this T entity, string name, object value) where T : HtmlEntity {
            entity.SetAttributeInternal(name, value);
            return entity;
        }
    }
}
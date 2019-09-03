using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlElement : HtmlBase {

        private string _tag;
        public string Text { get; set; }
        private List<HtmlBase> _children = new List<HtmlBase>();
        private List<HtmlAttribute> _attributes = new List<HtmlAttribute>();

        public string ToHtml() {
            using (StringWriter writer = new StringWriter()) {
                ToHtml(writer, 0);
                return writer.ToString();
            }
        }

        public HtmlElement(string tag, string text = null) {
            _tag = tag;
            Text = text;
        }

        public HtmlElement(string tag, params HtmlBase[] children) {
            _tag = tag;
            _children = children.ToList();
        }

        internal void SetAttributeInternal(string name, object value) {
            _attributes.Add(new HtmlAttribute(name, value));
        }

        public T Add<T>(T child) where T : HtmlBase {
            _children.Add(child);
            return child;
        }

        // public HtmlRaw Add(string rawHtml) {
        //     HtmlRaw raw = new HtmlRaw(rawHtml);
        //     _children.Add(raw);
        //     return raw;
        // }

        public override void ToHtml(TextWriter writer, int indent) {
            bool multiline = _children.Count > 0;

            // Opening Tag
            WriteIndent(writer, indent);
            WriteOpeningTag(writer, _tag, indent);
            if (multiline)
                writer.WriteLine();

            // Text Content
            if (Text != null)
                writer.Write(Text);

            // Children
            foreach (HtmlBase child in _children)
                if (child != null)
                    child.ToHtml(writer, indent + 1);

            // Closing Tag
            if (multiline)
                WriteIndent(writer, indent);
            WriteClosingTag(writer, _tag);
            writer.WriteLine();
        }

        private void WriteOpeningTag(TextWriter writer, string tag, int indent) {
            writer.Write("<" + tag);
            foreach (HtmlAttribute attribute in _attributes) {
                writer.Write(" ");
                attribute.Render(writer);
            }
            writer.Write(">");
        }

        private void WriteClosingTag(TextWriter writer, string tag) {
            writer.Write(string.Format("</{0}>", tag));
        }
    }

    public static class HtmlEntityExtensions {
        public static T Attr<T>(this T entity, string name, object value) where T : HtmlElement {
            entity.SetAttributeInternal(name, value);
            return entity;
        }

        public static T Class<T>(this T entity, string className) where T : HtmlElement {
            entity.SetAttributeInternal("class", className);
            return entity;
        }
    }
}
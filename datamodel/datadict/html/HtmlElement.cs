using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlElement : HtmlBase {

        private string _tag;
        private string _text;
        private List<HtmlBase> _children = new List<HtmlBase>();
        private List<HtmlAttribute> _attributes = new List<HtmlAttribute>();

        public string ToHtml() {
            using (StringWriter writer = new StringWriter()) {
                ToHtml(writer);
                return writer.ToString();
            }
        }

        public HtmlElement(string tag, string text = null) {
            _tag = tag;
            _text = text;
        }

        public HtmlElement(string tag, params HtmlBase[] children) {
            _tag = tag;
            _children = children.ToList();
        }

        internal void SetAttributeInternal(string name, object value) {
            _attributes.Add(new HtmlAttribute(name, value));
        }

        public T Add<T>(T child) where T : HtmlElement {
            _children.Add(child);
            return child;
        }

        public override void ToHtml(TextWriter writer) {
            WriteOpeningTag(writer, _tag);

            if (_text != null)
                writer.Write(_text);

            foreach (HtmlBase child in _children)
                if (child != null)
                    child.ToHtml(writer);

            WriteClosingTag(writer, _tag);
        }

        private void WriteOpeningTag(TextWriter writer, string tag) {
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
    }
}
using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlTag : HtmlEntity {

        private string _tag;
        private List<HtmlEntity> _children;

        public HtmlTag(string tag) {
            _tag = tag;
        }

        public T Add<T>(T child) where T : HtmlEntity {
            if (_children == null)
                _children = new List<HtmlEntity>();

            _children.Add(child);
            return child;
        }

        override public void ToHtml(TextWriter writer) {
            WriteOpeningTag(writer, _tag);
            foreach (HtmlEntity child in _children)
                child.ToHtml(writer);
            WriteClosingTag(writer, _tag);
        }
    }
}
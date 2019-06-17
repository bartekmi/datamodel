using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {

    public enum ImgScale {
        FALSE,
        TRUE,
        WIDTH,
        HEIGHT,
        BOTH
    }

    public class HtmlImg : HtmlEntity {

        private string _source;
        private ImgScale _scale;

        public HtmlImg(string source, ImgScale scale = ImgScale.BOTH) {
            _source = source;
            _scale = scale;
        }

        override public void ToHtml(TextWriter writer) {
            writer.Write("<IMG SCALE=\"{0}\" SRC=\"{1}\"/>", _scale, _source);
        }
    }
}
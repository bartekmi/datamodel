using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public class Node : GraphEntity {
        public string Name { get; set; }

        override public void ToDot(TextWriter writer) {
            writer.Write(ToID(Name));
            writer.Write(" ");
            WriteAttributes(writer);
        }
    }
}
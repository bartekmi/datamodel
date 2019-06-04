using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public class Edge : GraphEntity {

        public Node Source { get; set; }
        public Node Destination { get; set; }

        override public void ToDot(TextWriter writer) {
            writer.Write(string.Format("  {0} -> {1} "), Source.Name, Destination.Name);
            WriteAttributes(writer);
        }
    }
}
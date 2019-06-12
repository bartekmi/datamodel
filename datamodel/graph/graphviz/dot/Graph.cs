using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace datamodel.graphviz.dot {
    public class Graph : GraphEntity {

        private List<Node> _nodes = new List<Node>();
        private List<Edge> _edges = new List<Edge>();

        public void AddNode(Node node) {
            _nodes.Add(node);
        }

        public void AddEdge(Edge edge) {
            _edges.Add(edge);
        }

        public override void ToDot(TextWriter writer) {
            writer.WriteLine("digraph {");
            writer.Write("graph ");
            WriteAttributes(writer);

            foreach (Node node in _nodes)
                node.ToDot(writer);

            foreach (Edge edge in _edges)
                edge.ToDot(writer);

            writer.WriteLine("}");
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public abstract class GraphBase : GraphEntity {

        public string Name { get; set; }

        private string _dotName;
        private List<Node> _nodes = new List<Node>();
        private List<Edge> _edges = new List<Edge>();
        private List<Subgraph> _subgraphs = new List<Subgraph>();

        protected GraphBase(string dotName) {
            _dotName = dotName;
        }

        public void AddNode(Node node) {
            _nodes.Add(node);
        }

        public void AddEdge(Edge edge) {
            _edges.Add(edge);
        }

        public void AddSubgraph(Subgraph subgraph) {
            _subgraphs.Add(subgraph);
        }

        public override void ToDot(TextWriter writer) {
            writer.WriteLine(string.Format("{0} {1} {{", _dotName, ToID(Name)));

            writer.Write("graph ");     // This is for both graph and subgraph
            WriteAttributes(writer);

            foreach (Node node in _nodes)
                node.ToDot(writer);

            foreach (Edge edge in _edges)
                edge.ToDot(writer);

            foreach (Subgraph subgraph in _subgraphs)
                subgraph.ToDot(writer);

            writer.WriteLine("}");
        }
    }
}
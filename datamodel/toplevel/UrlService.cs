using System.Collections.Generic;

using datamodel.utils;
using datamodel.schema;
using datamodel.metadata;

namespace datamodel.toplevel {
    public class UrlService {

        private Dictionary<Model, List<GraphDefinition>> _modelToGraphs = new Dictionary<Model, List<GraphDefinition>>();

        private static UrlService _service = new UrlService();
        public static UrlService Singleton { get { return _service; } }

        private UrlService() { }     // Hide constructor

        public string DocUrl(Model model) {
            return UrlUtils.ToAbsolute(string.Format("{0}/{1}.html", model.Level1, model.SanitizedClassName));
        }

        // Return a list of generated graphs which contain the given model as a Core Model
        // from largets to smallest
        public List<GraphDefinition> GetGraphs(Model model) {
            return _modelToGraphs[model];
        }

        internal void AddGraph(GraphDefinition graph) {
            foreach (Model model in graph.CoreModels) {
                if (!_modelToGraphs.TryGetValue(model, out List<GraphDefinition> graphs)) {
                    graphs = new List<GraphDefinition>();
                    _modelToGraphs[model] = graphs;
                }
                graphs.Add(graph);
            }
        }
    }
}
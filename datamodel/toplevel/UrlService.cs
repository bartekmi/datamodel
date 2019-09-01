using System.Collections.Generic;

using datamodel.utils;
using datamodel.schema;

namespace datamodel.toplevel {
    public class UrlService {

        private Dictionary<Table, List<GraphDefinition>> _modelToGraphs = new Dictionary<Table, List<GraphDefinition>>();

        private static UrlService _service = new UrlService();
        public static UrlService Singleton { get { return _service; } }

        private UrlService() { }     // Hide constructor

        public string DocUrl(Table model) {
            return UrlUtils.ToAbsolute(string.Format("{0}/{1}.html", model.Team, model.SanitizedClassName));
        }

        // Return a list of generated graphs which contain the given model as a Core Model
        // from largets to smallest
        public List<GraphDefinition> GetGraphs(Table model) {
            return _modelToGraphs[model];
        }

        internal void AddGraph(GraphDefinition graph) {
            foreach (Table model in graph.CoreTables) {
                if (!_modelToGraphs.TryGetValue(model, out List<GraphDefinition> graphs)) {
                    graphs = new List<GraphDefinition>();
                    _modelToGraphs[model] = graphs;
                }
                graphs.Add(graph);
            }
        }
    }
}
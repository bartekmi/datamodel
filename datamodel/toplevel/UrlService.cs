using System.Collections.Generic;
using System.Linq;

using datamodel.utils;
using datamodel.schema;
using datamodel.metadata;

namespace datamodel.toplevel {
    public class UrlService {
        // If a model does not have a root level, put the data dictionary in this subdirectory
        public const string DATA_DICT_SUBDIR = "datadict";

        private Dictionary<Model, List<GraphDefinition>> _modelToGraphs = new();

        private static readonly UrlService _service = new();
        public static UrlService Singleton { get { return _service; } }

        private UrlService() { }     // Hide constructor

        public string DocUrl(Model model, bool fromNested) {
            if (model == null)
                return null;

            return UrlUtils.MakeUrl(
                string.Format("{0}{1}.html", 
                    (model.RootLevel ?? DATA_DICT_SUBDIR) + "/", 
                    model.SanitizedQualifiedName),
                fromNested);
        }

        // Return a list of generated graphs which contain the given model as a Core Model
        // from largets to smallest
        public List<GraphDefinition> GetGraphs(Model model) {
            if (_modelToGraphs.TryGetValue(model, out List<GraphDefinition> graphs))
                return graphs;
            return new List<GraphDefinition>();
        }

        private const int MAX_REASONABLE_GRAPH_SIZE = 35;   // Move to ENV?
        public GraphDefinition GetReasonableGraph(Model model) {
            GraphDefinition reasonable = GetGraphs(model).FirstOrDefault(x => x.CoreModels.Length <= MAX_REASONABLE_GRAPH_SIZE);
            if (reasonable != null)
                return reasonable;

            return GetGraphs(model).LastOrDefault();    // Didn't find a "reasonable" one... return smallest available
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
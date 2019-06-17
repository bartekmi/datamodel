using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using datamodel.schema;
using datamodel.graphviz;

namespace datamodel.graph {
    // Eventually, put logic here which determines what graphs to generate based on teams or team divisions/groups
    public static class GraphGenerator {
        public static void Generate(string team, string path) {
            Schema schema = Schema.Singleton;

            Dictionary<string, Table> tables = schema.Tables
                .Where(x => x.Team == team)
                .ToDictionary(x => x.ClassName);

            List<Association> associations = schema.Associations
                .Where(x => tables.ContainsKey(x.Source) && tables.ContainsKey(x.Destination))
                .ToList();

            (new GraphvizGenerator()).GenerateGraph(path, tables.Values, associations);
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

using datamodel.schema;
using datamodel.graphviz;

namespace datamodel.graph {

    public static class GraphGenerator {

        // Generate the graphs. 
        public static void GenerateAll(params GraphDefinition[] graphDefinitions) {
            Schema schema = Schema.Singleton;

            foreach (GraphDefinition graphDef in graphDefinitions) {
                string[] errors = graphDef.Validate();
                if (errors != null && errors.Length > 0)
                    throw new Exception(string.Join(", ", errors));
                if (!graphDef.IsTeamGraph && !graphDef.IsEngineGraph)
                    throw new Exception("Either Team or Engine must be specified");

                IEnumerable<Table> tables = string.IsNullOrWhiteSpace(graphDef.Team) ?
                    schema.Tables.Where(x => x.Engine == graphDef.Engine) :
                    schema.Tables.Where(x => x.Team == graphDef.Team);

                IEnumerable<Table> extraTables = graphDef.ExtraTables();

                string graphName = graphDef.IsTeamGraph ? graphDef.Team : Path.GetFileName(graphDef.Engine);
                Generate(graphDef, graphName, tables, extraTables);
            }
        }

        public static void Generate(GraphDefinition graphDef, string team, IEnumerable<Table> tables, IEnumerable<Table> extraTables) {

            List<Table> allTables = tables.Union(extraTables).ToList();
            Dictionary<string, Table> tablesDict = allTables.ToDictionary(x => x.ClassName);

            List<Association> associations = Schema.Singleton.Associations
                .Where(x => tablesDict.ContainsKey(x.Source) && tablesDict.ContainsKey(x.Destination))
                .ToList();

            string dotPath = Path.Combine(Env.TEMP_DIR, team + ".dot");
            (new GraphvizGenerator()).GenerateGraph(graphDef, dotPath, tables, associations, extraTables);

            string svgPath = Path.Combine(Env.OUTPUT_ROOT_DIR, team + ".svg");
            GraphvizRunner.Run(dotPath, svgPath, graphDef.Style);
        }
    }
}

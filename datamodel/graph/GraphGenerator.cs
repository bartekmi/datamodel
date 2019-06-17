using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

using datamodel.schema;
using datamodel.graphviz;

namespace datamodel.graph {

    public static class GraphGenerator {

        public static void GenerateAll(params string[] teams) {
            Schema schema = Schema.Singleton;

            HashSet<string> teamNames = new HashSet<string>(schema.Tables.Select(x => x.Team));
            foreach (string team in teams)
                if (!teamNames.Contains(team))
                    throw new Exception("Unknown Team: " + team);

            foreach (var group in schema.Tables.GroupBy(x => x.Team)) {
                if (teams.Length > 0 && !teams.Contains(group.Key))
                    continue;

                Generate(group.Key, group);
            }
        }


        public static void Generate(string team, IEnumerable<Table> tables) {

            Dictionary<string, Table> tablesDict = tables.ToDictionary(x => x.ClassName);

            List<Association> associations = Schema.Singleton.Associations
                .Where(x => tablesDict.ContainsKey(x.Source) && tablesDict.ContainsKey(x.Destination))
                .ToList();

            string dotPath = Path.Combine(Program.TEMP_DIR, team + ".dot");
            (new GraphvizGenerator()).GenerateGraph(dotPath, tablesDict.Values, associations);

            string svgPath = Path.Combine(Program.OUTPUT_ROOT_DIR, team + ".svg");
            GraphvizRunner.Run(dotPath, svgPath);
        }
    }
}

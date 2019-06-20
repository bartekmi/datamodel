using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using datamodel.schema;

namespace datamodel.graph {

    public enum RenderingStyle {
        Dot,
        Neato,
        Fdp,
    }

    // This class encapsulates specifis about a particular diagram/graph. It contains things that do not 
    // belong in the schema itself. Examples:
    // * Color assignments
    // * Extra tables to bring in from outside the team/group
    // * Rules for which tables or columns to include or omit
    // The intention is that these definitions will eventually be read from a yaml config file.
    // This class exists to provide separation between the logical information contained in the schema yaml files
    // and the more subjective, esthetic and visual information that defines a diagram. The two sources are 
    // obviously combined to create the final product.
    public class GraphDefinition {
        // The Team for which this diagram is for
        public string Team { get; set; }

        public RenderingStyle Style { get; set; }

        // Preferred Edge Length, in inches (neato and fdp only)
        // https://www.graphviz.org/doc/info/attrs.html#d:len
        public double? Len { get; set; }

        // Minimum separation between nodes, in points (all except dot)
        // https://www.graphviz.org/doc/info/attrs.html#d:sep
        public double? Sep { get; set; }

        // Extra tables (defined by class names) to bring in from outside the team
        public string[] ExtraTableClassNames { get; set; }

        public GraphDefinition(string team) {
            Team = team;
        }

        public GraphDefinition() {
            Style = RenderingStyle.Dot;
        }

        public string[] Validate() {
            List<string> errors = new List<string>();

            if (!Schema.Singleton.TeamExists(Team))
                errors.Add(string.Format("Team '{0}' does not exist", Team));

            if (ExtraTableClassNames != null)
                foreach (string className in ExtraTableClassNames)
                    if (Schema.Singleton.FindByClassName(className) == null)
                        errors.Add("Unknown Class Name: " + className);

            return errors.ToArray();
        }

        internal IEnumerable<Table> ExtraTables() {
            if (ExtraTableClassNames == null)
                return new Table[0];
            return ExtraTableClassNames.Select(x => Schema.Singleton.FindByClassName(x));
        }
    }
}

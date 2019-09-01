using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using datamodel.schema;
using datamodel.utils;

namespace datamodel.toplevel {

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

        // What type of Graphviz style to use
        public RenderingStyle Style { get; set; }

        // Preferred Edge Length, in inches (neato and fdp only)
        // https://www.graphviz.org/doc/info/attrs.html#d:len
        public double? Len { get; set; }

        // Minimum separation between nodes, in points (all except dot renderer)
        // https://www.graphviz.org/doc/info/attrs.html#d:sep
        public double? Sep { get; set; }

        // Core tables (defined by class names) - full attributes will be shown
        public Table[] CoreTables { get; set; }

        // Extra tables (defined by class names) - no attributes will be shown
        public Table[] ExtraTables { get; set; }

        // Name components of this graph from general to specified - e.g. team/engine/module
        public string[] NameComponents { get; set; }


        // Derived
        public string Name { get { return NameComponents.Last(); } }
        public string FullyQualifiedName {      // Used for both filenames and URL's
            get { return string.Join("__", NameComponents).Replace(' ', '_'); }
        }
        public string SvgUrl { get { return UrlUtils.ToAbsolute(string.Format("{0}.svg", FullyQualifiedName)); } }


        public GraphDefinition() {
            Style = RenderingStyle.Dot;
            CoreTables = new Table[0];
            ExtraTables = new Table[0];
        }

        // Validation code will kick-in once we import these from YAML
        public string[] Validate() {
            List<string> errors = new List<string>();

            // Validate(errors, CoreTables);
            // Validate(errors, ExtraTables);

            return errors.ToArray();
        }

        private void Validate(List<string> errors, string[] tables) {
            foreach (string className in tables)
                if (Schema.Singleton.FindByClassName(className) == null)
                    errors.Add("Unknown Class Name: " + className);
        }
    }
}

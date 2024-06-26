﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using datamodel.schema;
using datamodel.utils;

namespace datamodel.metadata {

    public enum RenderingStyle {
        // This style essentially lays out its nodes on a grid. This is the default.
        Dot,
        // Like balls and springs - works with "energy"
        Neato,
        // Like balls and springs - works with "forces"
        Fdp,
    }

    // This class encapsulates specifis about a particular diagram/graph. It contains things that do not 
    // belong in the schema itself. Examples:
    // * Extra tables to bring in from outside
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
        public Model[] CoreModels { get; set; }

        // Extra tables (defined by class names) - no attributes will be shown
        public Model[] ExtraModels { get; set; }

        // Name components of this graph from general to specified - i.e. Level1, Level2, Level3
        public string[] NameComponents { get; set; }

        // Human-friendly name of the Graph
        public string HumanName { get; set; }

        // Color assigned to this graph (e.g. when showing on "goto" buttons)
        public string ColorString { get; set; }

        // Derived
        public string FullyQualifiedName {      // Used for both filenames and URL's
            get {
                if (NameComponents.Length == 0)
                    return "all-models";
                return NameUtils.CompoundToSafe(NameComponents);
            }
        }
        public string GetSvgUrl(bool fromNested) { 
            return UrlUtils.MakeUrl(string.Format("{0}.svg", FullyQualifiedName), fromNested); 
        }


        public GraphDefinition() {
            Style = RenderingStyle.Dot;
            CoreModels = new Model[0];
            ExtraModels = new Model[0];
        }

        public bool HasSameNameAs(IEnumerable<string> other) {
            return
                string.Join("|", NameComponents) ==
                string.Join("|", other);
        }
    }
}

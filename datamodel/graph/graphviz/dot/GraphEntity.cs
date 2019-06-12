using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.graphviz.dot {
    public abstract class GraphEntity {
        private List<GV_Attribute> _attributes = new List<GV_Attribute>();

        public abstract void ToDot(TextWriter writer);

        public void SetAttributeInternal(string name, object value) {
            _attributes.Add(new GV_Attribute(name, value));
        }

        protected void WriteAttributes(TextWriter writer) {
            writer.Write("[");
            foreach (GV_Attribute attribute in _attributes) {
                attribute.ToDot(writer);
                writer.Write(" ");
            }
            writer.WriteLine("]");
        }
    }

    // Allows chaining
    public static class GraphEntityExtensions {
        public static T SetAttrGraph<T>(this T entity, string name, object value) where T : GraphEntity {
            entity.SetAttributeInternal(name, value);
            return entity;
        }
    }
}
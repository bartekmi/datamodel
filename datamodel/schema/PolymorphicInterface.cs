using System;

namespace datamodel.schema {
    public class PolymorphicInterface {
        // Derived
        public Column Column { get { throw new NotImplementedException(); } }
        public Model Model { get { return Column.Owner; } }
        public string Name { get { throw new NotImplementedException(); } }

        internal PolymorphicInterface() {
        }

        public override string ToString() {
            return Name;
        }
    }
}
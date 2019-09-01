using System;

namespace datamodel.schema {
    public class PolymorphicInterface {
        public RailsAssociation BelongsToAssociation { get; private set; }

        // Derived
        public Column Column { get { return BelongsToAssociation.FkColumn; } }
        public Model Model { get { return Column.Owner; } }
        public string Name { get { return BelongsToAssociation.Name; } }

        internal PolymorphicInterface(RailsAssociation association) {
            BelongsToAssociation = association;
            if (association.Name == null)
                throw new Exception("Polymorphic association does not have name");
        }
    }
}
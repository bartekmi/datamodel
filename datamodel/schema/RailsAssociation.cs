using System;
using System.Linq;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using datamodel.utils;

namespace datamodel.schema {

    internal enum AssociationKind {
        BelongsTo,
        HasOne,
        HasMany,
        HasAndBelongsToMany,
        Through,
    }

    public class RailsAssociation {

        internal AssociationKind Kind;
        internal string Name;
        internal string ActiveRecord;
        internal string ClassName;
        internal string ForeignKey;
        internal string ForeignType;
        internal string InverseName;
        internal string Klass;
        internal string PluralName;
        internal string Type;
        internal Options Options;
    }

    public class Options {
        internal bool Polymorphic;
        internal string ClassName;
        internal string As;
        internal bool Destroy;
        internal string InverseOf;
        internal string Through;
        internal string ForeignKey;
        internal string Source;
    }
}

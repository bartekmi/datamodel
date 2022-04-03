using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace datamodel.schema.source.from_data {

    public enum ElementType {
        Primitive,
        Object,
        Array
    }

    // "SDSS" = "Sample Data Schema Source"
    // Normally, I would have an abstract base class and three derived classes, but
    // using a single concrete class because I may need to switch from Object to Array
    // in the case of objects where key is data.
    public class SDSS_Element {
        private ElementType _type;
        public ElementType Type { get { return _type; } }

        // For primitive
        public string Value {get; private set;}
        public string DataType { get; private set; }

        // For object
        private Dictionary<string, SDSS_Element> _objectItems;
        public ReadOnlyDictionary<string, SDSS_Element> ObjectItems { get; private set; }

        // For array
        public IEnumerable<SDSS_Element> ArrayItems { get; set; }

        public SDSS_Element(ElementType type) {
            _type = type;

            if (_type == ElementType.Object) {
                _objectItems = new Dictionary<string, SDSS_Element>();
                ObjectItems = new ReadOnlyDictionary<string, SDSS_Element>(_objectItems);
            }
        }

        public SDSS_Element(IEnumerable<SDSS_Element> arrayItems) {
            _type = ElementType.Array;
            ArrayItems = arrayItems;
        }

        public SDSS_Element(string value, string dataType) {
            _type = ElementType.Primitive;
            Value = value;
            DataType = dataType;
        }

        public bool IsPrimitive { get { return _type == ElementType.Primitive; } }
        public bool IsObject { get { return _type == ElementType.Object; } }
        public bool IsArray { get { return _type == ElementType.Array; } }

        public void AddKeyValue(string key, SDSS_Element element) {
            if (Type != ElementType.Object)
                throw new Exception("Only use if type is Object");
            _objectItems[key] = element;
        }

        // There are many cases where arrays masquarade as objects, where each
        // key in the object is really just a field in the object that it contains.
        // This method re-converts such an object to the array that it really is.
        internal void ConvertObjectToArray(string keyProperty) {
            if (_type != ElementType.Object) 
                throw new Exception("Can only call on Object");

            _type = ElementType.Array;
            List<SDSS_Element> arrayItems = new List<SDSS_Element>();
            foreach (var item in ObjectItems) {
                if (item.Value.Type != ElementType.Object)
                    throw new NotImplementedException("Can only handle if children are objects");

                SDSS_Element keyAttr = new SDSS_Element(item.Key, "string");
                item.Value.AddKeyValue(keyProperty, keyAttr);
                arrayItems.Add(item.Value);
            }

            _objectItems = null;
            ArrayItems = arrayItems;
        }

        public override string ToString() {
            switch (_type) {
                case ElementType.Primitive: return Value;
                case ElementType.Object: return "Object...";
                case ElementType.Array: return "Array...";
                default:
                    throw new Exception("Added enum; did not update code?");
            }
        }
    }
}

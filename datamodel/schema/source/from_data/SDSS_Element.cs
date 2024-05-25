using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

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
        [JsonIgnore]    // Redundant - can be easily seen from what's populated
        public ElementType Type { get { return _type; } }

        // For primitive
        public string Value { get; private set; }
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

        // Derived
        [JsonIgnore]
        public bool IsPrimitive { get { return _type == ElementType.Primitive; } }
        [JsonIgnore]
        public bool IsObject { get { return _type == ElementType.Object; } }
        [JsonIgnore]
        public bool IsArray { get { return _type == ElementType.Array; } }
        [JsonIgnore]
        public bool IsEmptyArray { get { return IsArray && !ArrayItems.Any(); } }

        public void AddKeyAndValue(string key, SDSS_Element element) {
            if (Type != ElementType.Object)
                throw new Exception("Only use if type is Object");
            _objectItems[key] = element;
        }

        public void AddKeyAndValue(string key, string value) {
            AddKeyAndValue(key, new SDSS_Element(value, "string"));
        }

        // There are many cases where what logically are really arrays masquarade as objects, 
        // where each key in the object is really just a field in the object that it points to.
        // This method re-converts such an object to the array that it really is.
        internal void ConvertObjectToArray(string keyProperty) {
            if (_type != ElementType.Object)
                throw new Exception("Can only call on Object");

            List<SDSS_Element> arrayItems = new List<SDSS_Element>();

            foreach (var keyAndItem in ObjectItems) {
                string key = keyAndItem.Key;
                SDSS_Element item = keyAndItem.Value;

                if (item.Type == ElementType.Object) {
                    item.AddKeyAndValue(keyProperty, key);
                    arrayItems.Add(item);
                } else if (item.Type == ElementType.Array ||
                           item.Type == ElementType.Primitive) {
                    SDSS_Element intermediate = new(ElementType.Object);
                    intermediate.AddKeyAndValue(keyProperty, key);
                    intermediate.AddKeyAndValue("Value", item);
                    arrayItems.Add(intermediate);
                } else
                    throw new NotImplementedException("Unexpected element type: " + item.Type);
            }

            _objectItems = null;
            ObjectItems = null;
            _type = ElementType.Array;
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

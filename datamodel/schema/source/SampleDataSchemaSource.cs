using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using datamodel.schema.tweaks;

namespace datamodel.schema.source {
    // This is a base class for schema sources which try to re-create a schema from multiple
    // structured sample data files, such as JSON, YAML or XML  
    // The rules are simple:
    // - the name of the root object is passed in
    // - every internal defined object is named by the property which holds it
    // - primitive attributes become attributes of their enclosing scope object
    // - nested objects become owned associations
    // - nested lists become "many" associations
    // - qualified object name is based on path to reach the object (except for root, which is given explicitly)
    // - mark properties non-nullable if they are always present
    //
    // Potential for improvement
    // - find instances which are identical or similar and merge them
    // - recognize and extract enums (no spaces, a large-enough proportion of non-unique values)
    // - same paths that lead to atomic props with different primitive types => string | integer | boolean
    // - mark associations as ZeroOrOne vs One depending if they are always present
    //
    // Open questions
    // - How to treat same paths with (very) different properties - e.g representing inheritance
    public abstract class SampleDataSchemaSource : SchemaSource {
        protected abstract SDSS_Element GetRaw(string text);

        internal TempSource _source = new TempSource();     // Internal for testing
        private string _rootObjectName;
        private HashSet<string> _pathsWhereKeyIsData = new HashSet<string>();
        private bool _sameNameIsSameModel;
        private Dictionary<Model, int> _models = new Dictionary<Model, int>();
        private Dictionary<Column, int> _columns = new Dictionary<Column, int>();

        private const string KEY_COLUMN = "__key__";

        public class Options {
            public string RootObjectName { get; set; }
            public string[] PathsWhereKeyIsData { get; set; }
            // If true, any Model located at the same attribute name is considered to be identical
            public bool SameNameIsSameModel { get; set; }
        }

        public SampleDataSchemaSource(string[] filenames, Options options) {
            _rootObjectName = options.RootObjectName;
            _sameNameIsSameModel = options.SameNameIsSameModel;

            foreach (string path in options.PathsWhereKeyIsData) {
                _pathsWhereKeyIsData.Add(path);
                Model model = MaybeCreateModel(path);
            }

            foreach (string filename in filenames) {
                string text = File.ReadAllText(filename);
                SDSS_Element root = GetRaw(text);
                ParseObjectOrArray(root, "root");
            }

            SetCanBeEmpty();
            SetModelInstanceCounts();
        }

        public void ParseObjectOrArray(SDSS_Element token, string path) {
            if (token is SDSS_Array array) {
                foreach (SDSS_Element item in array) {
                    ParseObjectOrArray(item, path);
                }
            } else if (token is SDSS_Object obj) {
                // If this objects fields appear to be data, take special action
                if (IsKeyData(obj, path)) {
                    foreach (KeyValuePair<string, SDSS_Element> item in obj.Items) {
                        if (item.Value is SDSS_Object childObj) {
                            Model model = ParseObject(childObj, path);
                            if (model.FindColumn(KEY_COLUMN) == null)
                                AddKeyColumn(model, item.Key);
                        } else {
                            // TODO: Warn
                        }
                    }
                } else
                    ParseObject(obj, path);
            } else
                throw new Exception("Expected Array or Object, but got: " + token.Type);
        }

        private void AddKeyColumn(Model model, string example) {
            Column column = new Column() {
                Name = KEY_COLUMN,
                DataType = "String",
                CanBeEmpty = false,
                Owner = model,
            };
            model.AllColumns.Insert(0, column);

            if (example != null)
                column.AddLabel("Example", example);
        }

        private Model MaybeCreateModel(string path) {
            Model model = _source.FindModel(path);
            if (model == null) {
                model = new Model() {
                    QualifiedName = path,
                    Name = path.Split('.').Last(),
                };
                _models[model] = 0;
                _source.AddModel(model);
            }

            _models[model]++;

            return model;
        }

        private Model ParseObject(SDSS_Object obj, string path) {
            Model model = MaybeCreateModel(path);

            foreach (KeyValuePair<string, SDSS_Element> item in obj.Items) {
                string newPath = _sameNameIsSameModel ?
                    item.Key :
                    string.Format("{0}.{1}", path, item.Key);

                SDSS_Element token = item.Value;
                if (token is SDSS_Array array) {
                    // Array of primitive is treated like a primitive value
                    if (IsPrimitive(array.FirstOrDefault())) {
                        MaybeAddAttribute(model, item.Key, array.FirstOrDefault(), true);
                        continue;
                    }

                    MaybeAddAssociation(model, newPath, true);
                    ParseObjectOrArray(array, newPath);
                } else if (item.Value is SDSS_Object child) {
                    ParseObjectOrArray(token, newPath);
                    MaybeAddAssociation(model, newPath, false);
                } else {
                    MaybeAddAttribute(model, item.Key, item.Value, false);
                }
            }

            return model;
        }

        internal static Regex PROP_NAME_REGEX = new Regex("^[_$a-zA-Z][-_$a-zA-Z0-9]*$");
        // In some cases, rather than an object having ordinary properties, they key of the object actually
        // constitutes data. Kubernetes Swagger has lots of this.
        internal bool IsKeyData(SDSS_Object obj, string path) {
            if (_pathsWhereKeyIsData.Contains(path))
                return true;

            if (obj.Items.Count() > 50 ||       // Obviously, this is a suspect heuristic and should be (at the very least) injectable
                obj.Items.Keys.Any(x => !PROP_NAME_REGEX.IsMatch(x))) {

                _pathsWhereKeyIsData.Add(path);
                return true;
            }

            return false;
        }

        private bool IsPrimitive(SDSS_Element token) {
            if (token is SDSS_Array || token is SDSS_Object)
                return false;
            return true;
        }

        private void MaybeAddAssociation(Model model, string path, bool isMany) {
            Association assoc = _source.Associations
                .SingleOrDefault(x => x.OwnerSide == model.QualifiedName && x.OtherSide == path);

            if (assoc == null)
                _source.Associations.Add(new Association() {
                    OwnerSide = model.QualifiedName,
                    OwnerMultiplicity = Multiplicity.Aggregation,
                    OtherSide = path,
                    OtherMultiplicity = isMany ? Multiplicity.Many : Multiplicity.ZeroOrOne,
                });
            else {
                // TODO: If assoc exists, test if any of the parameters different from what has been recorded
            }
        }

        private void MaybeAddAttribute(Model model, string name, SDSS_Element token, bool isMany) {
            Column column = model.FindColumn(name);
            if (column == null) {
                column = new Column() {
                    Name = name,
                    DataType = GetDataType(token, isMany),
                    Owner = model,
                };

                if (token != null)
                    column.AddLabel("Example", token.ToString());

                model.AllColumns.Add(column);
                _columns[column] = 0;
            } else {
                string dataType = GetDataType(token, isMany);
                if (dataType != column.DataType)
                    Error.Log("Type mismatch on {0}.{1}: {2} vs {3}",
                        model.Name,
                        name,
                        dataType,
                        column.DataType);
            }

            _columns[column]++;
        }

        private string GetDataType(SDSS_Element token, bool isMany) {
            string type = token == null ? "unknown" : token.Type;
            if (isMany)
                type = "[]" + type;
            return type;
        }

        private void SetCanBeEmpty() {
            foreach (var item in _columns) {
                Column column = item.Key;
                int modelCount = _models[column.Owner];
                column.CanBeEmpty = item.Value < modelCount;
            }
        }

        private void SetModelInstanceCounts() {
            foreach (var item in _models)
                item.Key.AddLabel("Instance Count", item.Value.ToString());
        }

        #region Abstract Class Implementation
        public override string GetTitle() {
            return _rootObjectName;
        }

        public override IEnumerable<Model> GetModels() {
            return _source.Models.Values;
        }


        public override IEnumerable<Association> GetAssociations() {
            return _source.Associations;
        }
        #endregion
    }

    #region Helper Classes

    public abstract class SDSS_Element {
        public string Type { get; set; }
    }

    public class SDSS_Primitive : SDSS_Element {
        public string Value {get;set;}
        public override string ToString() {
            return Value;
        }
    }

    public class SDSS_Object : SDSS_Element {
        public Dictionary<string, SDSS_Element> Items { get; set; }
        public SDSS_Object() {
            Items = new Dictionary<string, SDSS_Element>();
        }
    }

    public class SDSS_Array : SDSS_Element, IEnumerable<SDSS_Element> {
        public IEnumerable<SDSS_Element> Items { get; set; }

        public IEnumerator<SDSS_Element> GetEnumerator() {
            return Items.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return Items.GetEnumerator();
        }
    }
    #endregion
}

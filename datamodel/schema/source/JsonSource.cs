using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using datamodel.schema.tweaks;

namespace datamodel.schema.source {
    // Extract a schema from a simple JSON document - not a JSON schema document or anything similar.
    // The rules are simple:
    // - the name of the root object is passed in
    // - every internall defined object is named by the property which holds it
    // - primitive JSON attributes become attributes of their enclosing scope object
    // - nested objects become mandatory owned associations
    // - nested lists become "many" associations
    // - qualified object name is based on JSON path to reach the object (except for root, which is given explicitly)
    //
    // Potential for improvement
    // - find instances which are identical or similar and merge them
    // - recognize and extract enums (no spaces, a large-enough proportion of non-unique values)
    // - mark an object nullable vs. nullable if it is always present
    // - same paths that lead to atomic props with different primitive types => string | integer | boolean
    //
    // Open questions
    // - How to treat same paths with (very) different properties
    public class JsonSource : SchemaSource {
        internal TempSource _source = new TempSource();     // Internal for testing
        private string _rootObjectName;
        private HashSet<string> _pathsWhereKeyIsData = new HashSet<string>();
        private bool _sameNameIsSameModel;
        private Dictionary<Model, int> _models = new Dictionary<Model, int>();
        private Dictionary<Column, int> _columns = new Dictionary<Column, int>();

        public class Options {
            public string RootObjectName { get; set; }
            public string[] PathsWhereKeyIsData { get; set; }
            // If true, any Model located at the same attribute name is considered to be identical
            public bool SameNameIsSameModel { get; set; }
        }

        public JsonSource(string filename, Options options) {
            _rootObjectName = options.RootObjectName;
            _sameNameIsSameModel = options.SameNameIsSameModel;

            foreach (string path in options.PathsWhereKeyIsData) {
                _pathsWhereKeyIsData.Add(path);
                MaybeCreateModel(path, true);
            }


            string json = File.ReadAllText(filename);
            object root = JsonConvert.DeserializeObject(json);

            ParseObjectOrArray(root as JToken, "root");
            SetCanBeEmpty();
            SetModelInstanceCounts();
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

        public void ParseObjectOrArray(JToken token, string path) {
            if (token is JArray array) {
                foreach (JToken item in array) {
                    ParseObjectOrArray(item, path);
                }
            } else if (token is JObject obj) {
                if (IsKeyData(obj, path)) {
                    foreach (KeyValuePair<string, JToken> item in obj) {
                        if (item.Value is JObject childObj)
                            ParseObject(childObj, path, true);
                        else {
                            // TODO: Warn
                        }
                    }
                } else
                    ParseObject(obj, path, false);
            } else
                throw new Exception("Expected Array or Object, but got: " + token.Type);

        }

        private Model MaybeCreateModel(string path, bool keyIsData) {
            Model model = _source.FindModel(path);
            if (model == null) {
                model = new Model() {
                    QualifiedName = path,
                    Name = path.Split('.').Last(),
                };
                _models[model] = 0;
                _source.AddModel(model);

                // If this objects fields appear to be data, take special action
                if (keyIsData) {
                    model.AllColumns.Add(new Column() {
                        Name = "__key__",
                        DataType = "String",
                        CanBeEmpty = false,
                        Owner = model,
                    });
                }
            }

            _models[model]++;

            return model;
        }

        private void ParseObject(JObject obj, string path, bool keyIsData) {
            Model model = MaybeCreateModel(path, keyIsData);

            foreach (KeyValuePair<string, JToken> item in obj) {
                string newPath = _sameNameIsSameModel ?
                    item.Key :
                    string.Format("{0}.{1}", path, item.Key);

                JToken token = item.Value;
                if (token is JArray array) {
                    // Array of primitive is treated like a primitive value
                    if (IsPrimitive(array.FirstOrDefault())) {
                        MaybeAddAttribute(model, item.Key, array.FirstOrDefault(), true);
                        continue;
                    }

                    MaybeAddAssociation(model, newPath, true);
                    ParseObjectOrArray(array, newPath);
                } else if (item.Value is JObject child) {
                    ParseObjectOrArray(token, newPath);
                    MaybeAddAssociation(model, newPath, false);
                } else {
                    MaybeAddAttribute(model, item.Key, item.Value, false);
                }
            }
        }

        internal static Regex PROP_NAME_REGEX = new Regex("^[_$a-zA-Z][-_$a-zA-Z0-9]*$");
        // In some cases, rather than an object having ordinary properties, they key of the object actually
        // constitutes data. Kubernetes Swagger has lots of this.
        internal bool IsKeyData(JObject objRaw, string path) {
            if (_pathsWhereKeyIsData.Contains(path))
                return true;

            var obj = (IDictionary<string, JToken>)objRaw;

            if (obj.Count() > 50 ||       // Obviously, this is a suspect heuristic and should be (at the very least) injectable
                obj.Keys.Any(x => !PROP_NAME_REGEX.IsMatch(x))) {

                _pathsWhereKeyIsData.Add(path);
                return true;
            }

            return false;
        }

        private bool IsPrimitive(JToken token) {
            if (token is JArray || token is JObject)
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

        private void MaybeAddAttribute(Model model, string name, JToken token, bool isMany) {
            Column column = model.FindColumn(name);
            if (column == null) {
                column = new Column() {
                    Name = name,
                    DataType = GetDataType(token, isMany),
                    Owner = model,
                };
                model.AllColumns.Add(column);
                _columns[column] = 0;
            } else {
                // TODO... At least warn if type mismatch
            }

            _columns[column]++;
        }

        private string GetDataType(JToken token, bool isMany) {
            string type = token == null ? "unknown" : token.Type.ToString();
            if (isMany)
                type = "[]" + type;
            return type;
        }

        public override string GetTitle() {
            return _rootObjectName;
        }

        public override IEnumerable<Model> GetModels() {
            return _source.Models.Values;
        }


        public override IEnumerable<Association> GetAssociations() {
            return _source.Associations;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using datamodel.schema.tweaks;

namespace datamodel.schema.source.from_data {
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
        private const string ROOT_PATH = "";
        private const string KEY_COLUMN = "__key__";

        protected abstract SDSS_Element GetRaw(string text);
        protected Options TheOptions;

        internal TempSource _source = new TempSource();     // Internal for testing
        private HashSet<string> _pathsWhereKeyIsData = new HashSet<string>();
        private Dictionary<Model, int> _models = new Dictionary<Model, int>();
        private Dictionary<Column, int> _columns = new Dictionary<Column, int>();

        public class Options {
            public string Title { get; set; }
            public string[] PathsWhereKeyIsData { get; set; }
            // If true, any Model located at the same attribute name is considered to be identical
            public bool SameNameIsSameModel { get; set; }

            // The minimum number of common properties in order for two sample files
            // to be considered part of the same "cluster".
            // By increasing this number, you can force files which accidentally have a few common
            // properties to still be considered separate clusters.
            // If you set this to zero, even files with no shared properties will be considered to
            // belong to the same cluster - so all files will be considered the same.
            public int MinimumClusterOverlap = 1;
        }

        public SampleDataSchemaSource(string[] filenames, Options options) {
            TheOptions = options ?? new Options();

            List<TempSource> clusters = ProcessFilesWithClustering(filenames, options);
            _source = CreateSeededSource(options);
            foreach (TempSource cluster in clusters)
                MergeSources(_source, cluster);

            SetCanBeEmpty();
            SetModelInstanceCounts();
        }

        private TempSource CreateSeededSource(Options options) {
            TempSource source = new TempSource();

            if (options?.PathsWhereKeyIsData != null)
                foreach (string path in options.PathsWhereKeyIsData) {
                    _pathsWhereKeyIsData.Add(path);
                    Model model = MaybeCreateModel(source, path, false);
                }

            return source;
        }

        #region Clustering
        // Algorithm:
        // 1. Iterate all files
        // 2a. Extract top-level model from each file
        // 2b. find overlap with each individual cluster - the parameter MIN_OVERLAP specifies the minimum overlap
        //    to be considered part of the same cluster - any smaller overlap is assumed to be accidental
        //    and does not constitute membership in a cluster.
        //    "Overlap" is defined as the count of shared QualifiedNames
        //
        // 3 => Next action determine by number of overlapping clusters...
        // 3a. One cluster overlap => add any new members to that cluster
        // 3b. Multiple overlaps => join the multiple clusters into one and add new members as above
        // 3c. Zero overlaps => We've discovered a new cluster... add it to _source
        private List<TempSource> ProcessFilesWithClustering(string[] filenames, Options options) {
            List<TempSource> clusters = new List<TempSource>();

            // 1 as above
            foreach (string filename in filenames) {
                // 2a as above
                string text = File.ReadAllText(filename);
                SDSS_Element root = GetRaw(text);

                TempSource candidate = CreateSeededSource(options);
                ParseObjectOrArray(candidate, root, ROOT_PATH);

                // 2b as above 
                List<TempSource> overlaps = new List<TempSource>();
                foreach (TempSource cluster in clusters) {
                    int overlap = CalculateOverlap(cluster, candidate);
                    if (overlap >= TheOptions.MinimumClusterOverlap)
                        overlaps.Add(cluster);
                }

                // 3 as above
                if (overlaps.Count == 1) {              // 3a
                    MergeSources(overlaps.Single(), candidate);
                } else if (overlaps.Count > 1) {        // 3b
                    TempSource first = overlaps.First();
                    foreach (TempSource other in overlaps.Skip(1)) {
                        MergeSources(first, other);
                        clusters.Remove(other);
                    }
                    MergeSources(overlaps.Single(), candidate);
                } else {                                // 3c
                    clusters.Add(candidate);
                }
            }

            PostProcessClusters(clusters);

            return clusters;
        }

        // Now that we have the final list of clusters, distinguish them by giving
        // all models and associations unique names within the cluster
        // Also, assign the first level
        private void PostProcessClusters(List<TempSource> clusters) {
            for (int ii = 0; ii < clusters.Count; ii++) {
                string clusterName = string.Format("cluster{0}", ii + 1);
                TempSource cluster = clusters[ii];

                foreach (Model model in cluster.GetModels()) {
                    model.QualifiedName = clusterName + model.QualifiedName;
                    model.SetLevel(0, clusterName);
                    if (model.Name == ROOT_PATH)
                        model.Name = clusterName;
                }

                foreach (Association assoc in cluster.GetAssociations()) {
                    assoc.OwnerSide = clusterName + assoc.OwnerSide;
                    assoc.OtherSide = clusterName + assoc.OtherSide;
                }
            }
        }

        private void MergeSources(TempSource main, TempSource additional) {
            // Merge models
            foreach (Model addModel in additional.Models.Values) {
                Model mainModel = main.FindModel(addModel.QualifiedName);
                if (mainModel == null)
                    main.AddModel(addModel);
                else
                    MergeModel(mainModel, addModel);
            }

            // Merge associations
            foreach (Association addAssoc in additional.Associations) {
                Association mainAssoc = main.Associations.SingleOrDefault(
                    x => x.OwnerSide == addAssoc.OwnerSide && x.OtherSide == addAssoc.OtherSide
                    // TODO: Ignoring multiplicity, etc
                );

                if (mainAssoc == null)
                    main.Associations.Add(addAssoc);
            }
        }

        private void MergeModel(Model main, Model additional) {
            _models[main] += _models[additional];
            _models.Remove(additional);

            foreach (Column addColumn in additional.AllColumns) {
                Column mainColumn = main.FindColumn(addColumn.Name);
                if (mainColumn == null) {
                    main.AllColumns.Add(addColumn);
                    addColumn.Owner = main;
                } else {
                    _columns[mainColumn] += _columns[addColumn];
                    _columns.Remove(addColumn);
                }
            }
        }

        private int CalculateOverlap(TempSource cluster, TempSource candidate) {
            int overlap = 0;
            foreach (Model clustModel in cluster.GetModels()) {
                Model candidateModel = candidate.FindModel(clustModel.QualifiedName);
                if (candidateModel == null)
                    continue;

                HashSet<string> clustColumns = new HashSet<string>(clustModel.AllColumns.Select(x => x.Name));
                overlap += clustColumns.Intersect(candidateModel.AllColumns.Select(x => x.Name)).Count();
            }

            // TODO: Also match on identical associations

            return overlap;
        }


        #endregion

        #region Recursive File Processing
        public void ParseObjectOrArray(TempSource source, SDSS_Element token, string path) {
            if (token is SDSS_Array array) {
                foreach (SDSS_Element item in array) {
                    ParseObjectOrArray(source, item, path);
                }
            } else if (token is SDSS_Object obj) {
                // If this objects fields appear to be data, take special action
                if (IsKeyData(obj, path)) {
                    foreach (KeyValuePair<string, SDSS_Element> item in obj.Items) {
                        if (item.Value is SDSS_Object childObj) {
                            Model model = ParseObject(source, childObj, path);
                            if (model.FindColumn(KEY_COLUMN) == null)
                                AddKeyColumn(model, item.Key);
                        } else {
                            // TODO: Warn
                        }
                    }
                } else
                    ParseObject(source, obj, path);
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

        private Model MaybeCreateModel(TempSource source, string path, bool addInstanceCount) {
            Model model = source.FindModel(path);
            if (model == null) {
                model = new Model() {
                    QualifiedName = path,
                    Name = path.Split('.').Last(),
                };
                _models[model] = 0;
                source.AddModel(model);
            }

            if (addInstanceCount)
                _models[model]++;

            return model;
        }

        private Model ParseObject(TempSource source, SDSS_Object obj, string path) {
            Model model = MaybeCreateModel(source, path, true);

            foreach (KeyValuePair<string, SDSS_Element> item in obj.Items) {
                string newPath = TheOptions.SameNameIsSameModel ?
                    item.Key :
                    string.Format("{0}.{1}", path, item.Key);

                SDSS_Element token = item.Value;
                if (token is SDSS_Array array) {
                    // Array of primitive is treated like a primitive value
                    if (IsPrimitive(array.FirstOrDefault())) {
                        MaybeAddAttribute(model, item.Key, array.FirstOrDefault(), true);
                        continue;
                    }

                    MaybeAddAssociation(source, model, newPath, true);
                    ParseObjectOrArray(source, array, newPath);
                } else if (item.Value is SDSS_Object child) {
                    ParseObjectOrArray(source, token, newPath);
                    MaybeAddAssociation(source, model, newPath, false);
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

        private void MaybeAddAssociation(TempSource source, Model model, string path, bool isMany) {
            Association assoc = source.Associations
                .SingleOrDefault(x => x.OwnerSide == model.QualifiedName && x.OtherSide == path);

            if (assoc == null)
                source.Associations.Add(new Association() {
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
        #endregion

        #region Post-Processing
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
        #endregion

        #region Abstract Class Implementation
        public override string GetTitle() {
            return TheOptions.Title;
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

    // "SDSS" = "Sample Data Schema Source"
    public abstract class SDSS_Element {
        public string Type { get; set; }
    }

    public class SDSS_Primitive : SDSS_Element {
        public string Value { get; set; }
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

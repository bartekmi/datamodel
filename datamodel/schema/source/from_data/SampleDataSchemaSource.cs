using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using datamodel.schema.tweaks;
using datamodel.utils;

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

        private const string UNKNOWN_DATA_TYPE = "unknown";

        protected abstract IEnumerable<SDSS_Element> GetRaw(PathAndContent file);
        protected Options TheOptions;

        internal TempSource _source = new();     // Internal for testing
        private Dictionary<Model, int> _models = new();
        private Dictionary<Property, int> _properties = new();
        private Dictionary<Association, int> _associations = new();

        #region Initialization
        public class Options {
            public string Title;
            public string[] PathsWhereKeyIsData;
            public bool SameNameIsSameModel;
            public int MinimumClusterOverlap;
            public Regex KeyIsDataRegex;
            public bool DisableKeyIsDataCheck;
        }

        public const string PARAM_PATHS = "paths";
        public const string PARAM_RAW = "raw";

        public const string PARAM_TITLE = "title";
        public const string PARAM_PATHS_WHERE_KEY_IS_DATA = "paths-where-key-is-data";
        public const string PARAM_SAME_NAME_IS_SAME_MODEL = "same-name-is-same-model";
        public const string PARAM_MINIMUM_CLUSTER_OVERLAP = "minimum-cluster-overlap";
        public const string PARAM_KEY_IS_DATA_REGEX = "key-is-data-regex";
        public const string PARAM_DISABLE_KEY_IS_DATA = "disable-key-is-data-check";

        public override void Initialize(Parameters parameters) {
            TheOptions = new Options() {
                Title = parameters.GetString(PARAM_TITLE),
                PathsWhereKeyIsData = parameters.GetStrings(PARAM_PATHS_WHERE_KEY_IS_DATA),
                SameNameIsSameModel = parameters.GetBool(PARAM_SAME_NAME_IS_SAME_MODEL),
                MinimumClusterOverlap = parameters.GetInt(PARAM_MINIMUM_CLUSTER_OVERLAP).Value, // Safe due to default
                KeyIsDataRegex = parameters.GetRegex(PARAM_KEY_IS_DATA_REGEX),
                DisableKeyIsDataCheck = parameters.GetBool(PARAM_DISABLE_KEY_IS_DATA),
            };

            string raw = parameters.GetString(PARAM_RAW);
            FileOrDir[] fileOrDirs = parameters.GetFileOrDirs(PARAM_PATHS);

            if (!(raw != null ^ fileOrDirs.Length > 0))
                throw new Exception(String.Format("Exactly one of these parameters must be set: {0}, {1}",
                    PARAM_RAW, PARAM_PATHS));

            IEnumerable<PathAndContent> files = new PathAndContent[] { new PathAndContent("from raw text", raw) };
            if (raw == null)
                files = FileOrDir.Combine(fileOrDirs);

            List<TempSource> clusters = ProcessFilesWithClustering(files);
            _source = new TempSource();
            foreach (TempSource cluster in clusters)
                MergeSources(_source, cluster);

            SetCanBeEmptyProperty();
            SetOtherMultiplicityProperty();
            SetModelInstanceCountLabel();
            SetListSemanticProperty();
        }
        #endregion

        #region Recursive File Processing
        public void ParseObjectOrArray(TempSource source, SDSS_Element element, string path) {
            if (element.IsArray) {
                foreach (SDSS_Element item in element.ArrayItems) {
                    ParseObjectOrArray(source, item, path);
                }
            } else if (element.IsObject) {
                ParseObject(source, element, path);
            } else {
              // This can happen if an array has mixed elements, which we are not handling yet.
              // If the array does NOT have mixed elements, this case would have been handled 
              // by IsPossiblyNestedArrayOfPrimitives()
            }
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

        private Model ParseObject(TempSource source, SDSS_Element obj, string path) {
            Model model = MaybeCreateModel(source, path, true);

            foreach (KeyValuePair<string, SDSS_Element> item in obj.ObjectItems) {
                string newPath = TheOptions.SameNameIsSameModel ?
                    item.Key :
                    string.Format("{0}.{1}", path, item.Key);

                SDSS_Element element = item.Value;
                if (element.IsArray) {
                    // (Possibly nested) Array of primitive is treated like a primitive value
                    if (IsPossiblyNestedArrayOfPrimitives(element, out SDSS_Element primitive, out int depth)) {
                        MaybeAddAttribute(model, item.Key, primitive, depth);
                        continue;
                    }

                    MaybeAddAssociation(source, model, newPath, true);
                    ParseObjectOrArray(source, element, newPath);
                } else if (item.Value.IsObject) {
                    ParseObjectOrArray(source, element, newPath);
                    MaybeAddAssociation(source, model, newPath, false);
                } else {
                    MaybeAddAttribute(model, item.Key, item.Value, 0);
                }
            }

            return model;
        }

        private bool IsPossiblyNestedArrayOfPrimitives(SDSS_Element array, out SDSS_Element primitive, out int depth) {
          depth = 0;
          primitive = null;

          while (true) {
            if (array.IsEmptyArray)
              return false;

            depth++;
            SDSS_Element first = array.ArrayItems.First();

            switch (first.Type) {
              case ElementType.Primitive:
                primitive = first;
                return true;
              case ElementType.Object:
                return false;
              case ElementType.Array:
                array = first;
                break;
              default:
                throw new Exception("Unexpected SDSS_Elemnt type: " + first.Type);
            }
          }
        }

        private void MaybeAddAssociation(TempSource source, Model model, string path, bool isMany) {
            Association assoc = source.Associations
                .SingleOrDefault(x => x.OwnerSide == model.QualifiedName && x.OtherSide == path);

            if (assoc == null) {
                assoc = new Association() {
                    OwnerSide = model.QualifiedName,
                    OwnerSideModel = model,
                    OwnerMultiplicity = Multiplicity.Aggregation,
                    OtherSide = path,
                    OtherMultiplicity = isMany ? Multiplicity.Many : Multiplicity.ZeroOrOne,
                };
                source.Associations.Add(assoc);
                _associations[assoc] = 0;
            }

            _associations[assoc]++;
        }

        private void MaybeAddAttribute(Model model, string name, SDSS_Element element, int depth) {
            Property property = model.FindProperty(name);
            if (property == null) {
                property = new Property() {
                    Name = name,
                    DataType = GetDataType(element, depth),
                    Owner = model,
                };

                model.AllProperties.Add(property);
                _properties[property] = 0;
            } else {
                string newDataType = GetDataType(element, depth);
                string newLower = newDataType.ToLower();
                string oldLower = property.DataType.ToLower();

                if (newDataType == UNKNOWN_DATA_TYPE ||
                    newLower == "integer" && oldLower == "float") {
                    // Do nothing... No new informatin to contribute
                } else if (
                        property.DataType == UNKNOWN_DATA_TYPE ||
                        oldLower == "integer" && newLower == "float")
                    property.DataType = newDataType;
                else    // Both old and new had a data type and they don't match
                    if (newDataType != property.DataType)
                        Error.Log("Type mismatch on {0}.{1}: New: {2} vs Previously found: {3}",
                            model.Name,
                            name,
                            newDataType,
                            property.DataType);
            }

            if (!property.HasLabel("Example") &&
                !string.IsNullOrWhiteSpace(element?.Value))
                    property.AddLabel("Example", element.ToString());

            _properties[property]++;
        }

        private string GetDataType(SDSS_Element element, int arrayDepth) {
            string type = string.IsNullOrWhiteSpace(element?.DataType) ? 
                UNKNOWN_DATA_TYPE : element.DataType;
                
            for (int ii = 0; ii < arrayDepth; ii++)
                type = "[]" + type;

            return type;
        }
        #endregion

        #region Clustering
        // Algorithm:
        // 1. For each file
        // 2a. Extract top-level samples
        private List<TempSource> ProcessFilesWithClustering(IEnumerable<PathAndContent> files) {
            List<TempSource> clusters = new();

            // 1 as above
            foreach (PathAndContent file in files) {
                try {
                    // 2a as above
                    IEnumerable<SDSS_Element> roots = GetRaw(file);
                    foreach (SDSS_Element root in roots)
                        ProcessSample(clusters, root);

                } catch (Exception e) {
                    throw new Exception(string.Format("Error while working on file '{0}'", file.Path), e);
                }
            }

            PostProcessClusters(clusters);

            return clusters;
        }

        // Algorithm:
        // 2b. find overlap with each individual cluster - the parameter MIN_OVERLAP specifies the minimum overlap
        //    to be considered part of the same cluster - any smaller overlap is assumed to be accidental
        //    and does not constitute membership in a cluster.
        //    "Overlap" is defined as the count of shared QualifiedNames
        //
        // 3 => Next action is determine by number of overlapping clusters...
        // 3a. One cluster overlap => We've found another instance of an existing cluster.
        //     Add any new members to that cluster.
        // 3b. Multiple overlaps => This instance is the "missing link" between the multiple clusters.
        //     Join the multiple clusters into one and add new members as above.
        // 3c. Zero overlaps => We've discovered a new cluster... add it to _source
        private void ProcessSample(List<TempSource> clusters, SDSS_Element root) {
            if (!TheOptions.DisableKeyIsDataCheck)
                SampleDataKeyIsData.ConvertObjectsWhereKeyIsData(TheOptions, root);

            TempSource candidate = new();
            ParseObjectOrArray(candidate, root, SampleDataKeyIsData.ROOT_PATH);

            // 2b as above 
            List<TempSource> overlaps = new();
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
            } else {                                // 3c
                clusters.Add(candidate);
            }
        }

        // Now that we have the final list of clusters, distinguish them by giving
        // all models and associations unique names within the cluster
        // Also, assign the first level
        private void PostProcessClusters(List<TempSource> clusters) {
            for (int ii = 0; ii < clusters.Count; ii++) {
                string clusterName = string.Format("cluster{0}", ii + 1);
                TempSource cluster = clusters[ii];

                foreach (Model model in cluster.GetModels()) {
                    model.QualifiedName = ComputeQualifiedName(clusterName, model.QualifiedName);
                    model.SetLevel(0, clusterName);
                    if (model.Name == SampleDataKeyIsData.ROOT_PATH)
                        model.Name = clusterName;
                }

                foreach (Association assoc in cluster.GetAssociations()) {
                    assoc.OwnerSide = ComputeQualifiedName(clusterName, assoc.OwnerSide);
                    assoc.OtherSide = ComputeQualifiedName(clusterName, assoc.OtherSide);
                }
            }
        }

        private string ComputeQualifiedName(string cluster, string name) {
          if (string.IsNullOrEmpty(name))
            return cluster;
          if (name.StartsWith('.'))
            return cluster + name;

          return string.Format("{0}.{1}", cluster, name);
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
                    // TODO: Ignoring multiplicity
                );

                if (mainAssoc == null) { // This is a newly-discovered association
                    main.Associations.Add(addAssoc);
                    addAssoc.OwnerSideModel = main.FindModel(addAssoc.OwnerSide);
                } else
                    MergeAssociation(mainAssoc, addAssoc);
            }
        }

        private void MergeAssociation(Association main, Association additional) {
            _associations[main] += _associations[additional];
            _associations.Remove(additional);

            // In some cases, an association may link to either a single object or many objects.
            // The following accounts for the situation where the first instance of the Association
            // links to a single object, but it turns out that the association is really one-to-many
            // as demostrated by a subsequent instance.
            if (additional.OtherMultiplicity == Multiplicity.Many)
                main.OtherMultiplicity = Multiplicity.Many;
        }

        private void MergeModel(Model main, Model additional) {
            _models[main] += _models[additional];
            _models.Remove(additional);

            foreach (Property addProperty in additional.AllProperties) {
                Property mainProperty = main.FindProperty(addProperty.Name);
                if (mainProperty == null) {
                    main.AllProperties.Add(addProperty);
                    addProperty.Owner = main;
                } else {
                    _properties[mainProperty] += _properties[addProperty];
                    _properties.Remove(addProperty);
                }
            }
        }

        private int CalculateOverlap(TempSource cluster, TempSource candidate) {
            int overlap = 0;
            foreach (Model clustModel in cluster.GetModels()) {
                Model candidateModel = candidate.FindModel(clustModel.QualifiedName);
                if (candidateModel == null)
                    continue;

                HashSet<string> clustProperties = new(clustModel.AllProperties.Select(x => x.Name));
                overlap += clustProperties.Intersect(candidateModel.AllProperties.Select(x => x.Name)).Count();
            }

            // TODO: Also match on identical associations

            return overlap;
        }


        #endregion

        #region Post-Processing
        private void SetCanBeEmptyProperty() {
            foreach (var item in _properties) {
                Property property = item.Key;
                int modelCount = _models[property.Owner];
                property.CanBeEmpty = item.Value < modelCount;
            }
        }

        private void SetOtherMultiplicityProperty() {
            foreach (var item in _associations) {
                Association association = item.Key;
                int modelCount = _models[association.OwnerSideModel];
                if (association.OtherMultiplicity == Multiplicity.ZeroOrOne && item.Value == modelCount)
                    association.OtherMultiplicity = Multiplicity.One;
            }
        }

        private void SetModelInstanceCountLabel() {
            foreach (var item in _models)
                item.Key.AddLabel("Instance Count", item.Value.ToString());
        }

        // Convert things like entry<>---Invoices<>---<Invoice into...
        //                     entry<>---<Invoice
        private void SetListSemanticProperty() {
            foreach (var item in _models) {
                Model model = item.Key;
                IEnumerable<Association> outgoing = _associations.Keys
                    .Where(x => x.OwnerSide == model.QualifiedName);

                if (model.AllProperties.Count == 0 && outgoing.Count() == 1) {
                    string childModelNameFQ = outgoing.Single().OtherSide;
                    string childModelName = childModelNameFQ.Split('.').Last();
                    string modelName = model.QualifiedName.Split('.').Last();
                    if (modelName == NameUtils.Pluralize(childModelName))
                        model.ListSemanticsForType = childModelNameFQ;
                }
            }
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

        public override IEnumerable<Parameter> GetParameters() {
            return new List<Parameter>() {
                new Parameter() {
                    Name = PARAM_RAW,
                    Description = @"Raw json/yaml/etc content - useful for testing",
                    Type = ParamType.String,
                },

                // Options
                new Parameter() {
                    Name = PARAM_TITLE,
                    Description = @"Title of the Schema",
                    Type = ParamType.String,
                },
                new Parameter() {
                    Name = PARAM_PATHS_WHERE_KEY_IS_DATA,
                    Description = @"JSON paths to attributes where we know that the key represents a data field",
                    Type = ParamType.String,
                    IsMultiple = true,
                },
                new Parameter() {
                    Name = PARAM_DISABLE_KEY_IS_DATA,
                    Description = @"If true, disable checking whether object key may represent data",
                    Type = ParamType.Bool,
                },
                new Parameter() {
                    Name = PARAM_SAME_NAME_IS_SAME_MODEL,
                    Description = @"If true, any Model located at the same attribute name, regardless of the path, is considered to be identical",
                    Type = ParamType.Bool,
                },
                new Parameter() {
                    Name = PARAM_MINIMUM_CLUSTER_OVERLAP,
                    Description = @"The minimum number of common properties in order for two sample files
        to be considered part of the same 'cluster'.
        By increasing this number, you can force files which accidentally have a few common
        properties to still be considered separate clusters.
        If you set this to zero, even files with no shared properties will be considered to
        belong to the same cluster - so all files will be considered the same.",
                    Type = ParamType.Int,
                    Default = "1",
                },
                new Parameter() {
                    Name = PARAM_KEY_IS_DATA_REGEX,
                    Description = @"If the key of an Object does NOT match this Regex, it will be assumed
        that all the keys of all the instances if this object should be 
        treated as data, and the Object itself should be treated as an Array.",
                    Type = ParamType.Regex,
                    Default = "^[$a-zA-Z][_$a-zA-Z0-9]*$"
                },
            };
        }
        #endregion
    }
}

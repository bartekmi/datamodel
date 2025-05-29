using System.Collections.Generic;
using System.Linq;
using datamodel.utils;
using Newtonsoft.Json;

using System;

namespace datamodel.schema.source.botocore;

public class BotoCoreSource : SchemaSource {
    private const string PARAM_FILE_OR_DIRS = "filesOrDirs";

    private string _title = "AWS Data Model From BotoCore files";
    private readonly List<Model> _models = [];
    private readonly List<Association> _associations = [];

    #region Members / Abstract implementations
    public BotoCoreSource() {
        Tweaks = [
            new RemoveLowValuesModels(),
        ];
    }

    public override void Initialize(Parameters parameters) {
        FileOrDir[] filesOrDirs = parameters.GetFileOrDirs(PARAM_FILE_OR_DIRS);

        foreach (FileOrDir fod in filesOrDirs)
            foreach (PathAndContent pac in fod.Files) {
                BotoService service = ParseFile(pac);
                if (filesOrDirs.Length == 1 && filesOrDirs[0].IsFile)
                    _title = service.Metadata.ServiceFullName;
            }
    }

    public override IEnumerable<Parameter> GetParameters() {
        return [
            new ParameterFileOrDir() {
                Name = PARAM_FILE_OR_DIRS,
                Description = "Comma-separated list of files and/or directories where to look for botocore json files",
                Type = ParamType.FileOrDir,
                IsMandatory = true,
                IsMultiple = true,
            },
        ];
    }

    public override string GetTitle() {
        return _title;
    }

    public override IEnumerable<Model> GetModels() {
        return _models;
    }

    public override IEnumerable<Association> GetAssociations() {
        return _associations;
    }
    #endregion

    #region Parsing / Extraction
    private BotoService ParseFile(PathAndContent pac) {
        BotoService service = JsonConvert.DeserializeObject<BotoService>(pac.Content);

        // Pass 1 - Remember unique names of all shapes
        Dictionary<string, BotoShape> namesToShapes = FindInterestingShapes(service);

        // Pass 2 - Create Models from the Shapes
        foreach (BotoShape shape in namesToShapes.Values)
            if (shape.IsStructure)
                ParseStructure(service, namesToShapes, shape);

        // Pass 3 - Create Associations
        foreach (BotoShape shape in namesToShapes.Values)
            if (shape.IsStructure)
                ExtractAssociations(service, namesToShapes, shape);

        return service;
    }

    /**
    * There are many shapes in every service that do not contribute to the
    * data model. For example, shapes that only serve as data input.
    */
    private static Dictionary<string, BotoShape> FindInterestingShapes(BotoService service) {
        // Step 0 - Hydration / house-keeping
        foreach (var shape in service.Shapes)
            shape.Value.ShapeName = shape.Key;  // Set the shape name of BotoShape

        // Step 1 - Find get/describe/list operations
        HashSet<BotoOperation> operations = service.Operations.Values
        .Where(x => x.IsListOp || x.IsGetOp)
        .ToHashSet();

        // Step 2 - remove list operations for which get/describe equivalents exist
        foreach (BotoOperation operation in operations.ToList()) {
            if (!operation.Name.StartsWith("List"))
                continue;

            string mainOpName = operation.Name.Substring("List".Length);
            mainOpName = NameUtils.Singluarize(mainOpName);

            if (service.Operations.ContainsKey("Get" + mainOpName) ||
                service.Operations.ContainsKey("Describe" + mainOpName)) {

                operations.Remove(operation);
            }
        }

        // Step 3 - Recursively process each operation to extract all its shapes
        Dictionary<string, BotoShape> namesToShapes = [];
        foreach (BotoOperation operation in operations) {
            string outputShapeName = operation.Output.Shape;
            BotoShape outputShape = service.Shapes[outputShapeName];

            // Sometimes, top-level structures are uninteresting, so ignore them
            // Top level struct of List output are boring
            if (operation.IsListOp) {
                BotoShape singleNestedShape = null;
                foreach (string nestedShapeName in outputShape.Members.Values.Select(x => x.Shape)) {
                    BotoShape nestedShape = service.Shapes[nestedShapeName];
                    if (nestedShape.IsList) {
                        if (singleNestedShape != null)
                            throw new NotImplementedException("How to deal with multiple lists at top level of List... operation?");
                        singleNestedShape = nestedShape;
                    }
                }
                if (singleNestedShape == null)
                    continue;       // E.g. ListTags operation
                outputShape = singleNestedShape;

                // Get/Describe output if it only contains a single nested structure is boring
            } else if (operation.IsGetOp && outputShape.Members.Count == 1) {
                string singleMemberShapeName = outputShape.Members.Values.Single().Shape;
                BotoShape singleMemberShape = service.Shapes[singleMemberShapeName];
                if (singleMemberShape.IsStructure)
                    outputShape = singleMemberShape;
            }

            outputShape.Labels["Output For"] = operation.Name;
            RecursivelyExtractShapes(service, namesToShapes, outputShape.ShapeName);
        }

        return namesToShapes;
    }

    private static void RecursivelyExtractShapes(
        BotoService service,
        Dictionary<string, BotoShape> namesToShapes,
        string shapeName
    ) {
        if (namesToShapes.ContainsKey(shapeName))
            return; // Nothing new to process

        BotoShape shape = service.Shapes[shapeName];
        namesToShapes[shapeName] = shape;

        string[] nestedShapeNames = null;
        switch (shape.Type) {
            case "structure":
                nestedShapeNames = shape.Members.Values.Select(x => x.Shape).ToArray();
                break;
            case "list":
                nestedShapeNames = [shape.Member.Shape];
                break;
            case "map":
                nestedShapeNames = [shape.Key.Shape, shape.Value.Shape];
                break;
        }

        if (nestedShapeNames != null)
            foreach (string nestedShapeName in nestedShapeNames)
                RecursivelyExtractShapes(service, namesToShapes, nestedShapeName);
    }

    private void ExtractAssociations(
        BotoService service,
        Dictionary<string, BotoShape> mappings,
        BotoShape structure
    ) {
        foreach (var kvp in structure.Members) {
            string memberName = kvp.Key;
            BotoShapeReference shapeRef = kvp.Value;
            BotoShape shape = mappings[shapeRef.Shape];

            if (shape.IsStructure) {
                Multiplicity multiplicity = structure.IsRequired(memberName)
                    ? Multiplicity.One : Multiplicity.ZeroOrOne;
                AddAssociation(service, structure, memberName, shapeRef, multiplicity);
            } else if (shape.IsList) {
                BotoShape memberShape = mappings[shape.Member.Shape];

                if (memberShape.IsPrimitive)
                    // If map value type is primitive, just treat this is a primitive field
                    structure.Model.AllProperties.Add(new() {
                        Name = memberName,
                        Description = shape.Documentation,
                        DataType = memberShape.Type + "[]",
                        CanBeEmpty = !structure.IsRequired(memberName),
                    });
                else
                    AddAssociation(service, structure, memberName, shape.Member, Multiplicity.Many);
            } else if (shape.IsMap) {
                BotoShape valueShape = mappings[shape.Value.Shape];

                if (valueShape.IsPrimitive || valueShape.IsListOfPrimitive(mappings))
                    // If map value type is primitive, just treat this is a primitive field
                    structure.Model.AllProperties.Add(new() {
                        Name = memberName,
                        Description = shape.Documentation,
                        DataType = "Map",
                        CanBeEmpty = !structure.IsRequired(memberName),
                    });
                else {
                    // TODO: There is an edge case where map values can be things other than structures 
                    // - e.g. lists. In that case, valueShape.Model is null. We do not yet handle this case.
                    if (valueShape.Model == null)
                        throw new NotImplementedException();

                    // Otherwise, we have to make some assumptions (at least for now)...
                    // 1. The type of the key has meaningful semantics. We add this key as a member
                    //    of the "Value" Model
                    // 2. This key type is being used consistently throughout the service
                    // TODO: Write code to verify these assumptions
                    BotoShape keyType = mappings[shape.Key.Shape];
                    valueShape.Model.AllProperties.Add(new() {
                        Name = keyType.ShapeName,
                        Description = "Name is derived from type of they key in a Map",
                        DataType = keyType.Type,
                    });
                    AddAssociation(service, structure, memberName, shape.Key, Multiplicity.Many);
                }
            }
        }
    }

    private void AddAssociation(
        BotoService service,
        BotoShape structure,
        string memberName,
        BotoShapeReference shapeRef,
        Multiplicity multiplicity
    ) {
        Association association = new() {
            OwnerSide = ToFullyQualifed(service, structure.ShapeName),
            OwnerMultiplicity = Multiplicity.Aggregation,

            OtherSide = ToFullyQualifed(service, shapeRef.Shape),
            OtherMultiplicity = multiplicity,
            OtherRole = memberName,

            Description = shapeRef.Documentation,
        };
        _associations.Add(association);
    }

    private static string ToFullyQualifed(BotoService service, string shapeName) {
        string prefix = service.Metadata.EndpointPrefix;
        return prefix + "." + shapeName;
    }

    private Model ParseStructure(
            BotoService service,
            Dictionary<string, BotoShape> mappings,
            BotoShape structure
        ) {
        string prefix = service.Metadata.EndpointPrefix;

        Model model = new() {
            Name = structure.ShapeName,
            QualifiedName = ToFullyQualifed(service, structure.ShapeName),
            Description = structure.Documentation,
            Levels = [prefix],
            AllProperties = GetProperties(structure, mappings, structure.Members)
        };

        model.Labels.AddRange(structure.Labels.Select(x => new Label(x.Key, x.Value)));
        _models.Add(model);
        structure.Model = model;

        return model;
    }

    private static List<Property> GetProperties(
        BotoShape structure,
        Dictionary<string, BotoShape> mappings,
        Dictionary<string, BotoShapeReference> members
    ) {
        List<Property> properties = [];
        if (members == null)
            return properties;

        foreach (var kvp in members) {
            BotoShape shape = mappings[kvp.Value.Shape];
            if (!shape.IsPrimitive)
                continue;

            Property property = new() {
                Name = kvp.Key,
                Description = kvp.Value.Documentation,
                DataType = shape.Enum == null ? shape.Type : "Enum",
                CanBeEmpty = !structure.IsRequired(kvp.Key),
                Enum = shape.Enum == null ? null : new(shape.Enum),
            };

            properties.Add(property);
        }

        return properties;
    }
    #endregion
}



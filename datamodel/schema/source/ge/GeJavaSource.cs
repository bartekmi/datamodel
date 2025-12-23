using System.Collections.Generic;

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace datamodel.schema.source.ge;

// To run this in Bartek's account:
// dotnet run ge dir=/Users/250023731/gitlab/docs-site/models/specification
public class GeJavaSource : SchemaSource {
    #region Members / Abstract 

    // Mapping of "targetField" key name to Entity name
    // Used to create associations from "Joins" where targetModel is same as the model 
    private readonly Dictionary<string, string> KEY_TO_ENTITY = new() {
        ["patientid"] = "Patient",
        ["patientidentifier"] = "Patient",
        ["mrn"] = "Patient",
        ["patientmrn"] = "Patient",

        ["visitid"] = "PatientVisit",
        ["visitnumber"] = "PatientVisit",
        ["encounterid"] = "PatientVisit",
        ["patientcsn"] = "PatientVisit",
        ["csn"] = "PatientVisit",

        ["appointmentid"] = "Appointment",

        ["facility"] = "FacilitiesLocationMaster",
        ["facilityid"] = "FacilitiesLocationMaster",
        ["facilitycode"] = "FacilitiesLocationMaster",
        ["unit"] = "UnitsLocationMaster",
        ["unitid"] = "UnitsLocationMaster",
        ["bed"] = "BedsLocationMaster",
        ["bedid"] = "BedsLocationMaster",

    };

    // These groupsing will do two things...
    // 1) Determine how "root" entities (and their children) are placed into Data Model diagrams
    // 2) Assign a unique color to each group
    private readonly Dictionary<string, List<string>> GROUP_TO_MODELS = new() {
        ["appointment"] = ["Appointment"],
        ["patient-visit"] = ["AvoidableNights", "DepartureLounge", "Device", "DischargeMilestones", "EmrFlags", "InfectiousDisease", "Patient", "PatientVisit", "PostAcute"],
        ["transfer"] = ["BedRequest", "TransferRequest", "TransportExternal", "TransportInternal"],
        ["miscellaneous"] = ["Equipment", "FlowSheet", "PressureForecast", "PliEditRequest", "PressureFactor"],
        ["personnel"] = ["HospitalPersonnel", "HospitalPersonnelSchedule", "HospitalPersonnelTimesheet"],
        ["location"] = ["InternalLocationMaster", "PressureScore", "DivertStatus", "BedsLocationMaster", "FacilitiesLocationMaster", "UnitsLocationMaster"],
        ["order"] = ["Order"],
        ["recommendation"] = ["ActionRecommendation"],
    };

    private Dictionary<string, ClassInfo> _javaModels = [];
    private Dictionary<string, ClassInfo> _javaEntities = [];
    private readonly List<Model> _models = [];
    private readonly List<Association> _associations = [];

    public override void Initialize(Parameters parameters) {
        // Load Models and Entitie4s
        _javaModels = GeJavaParser.LoadExtraction("/tmp/datamodel/java_models.yaml").classInfos
            .ToDictionary(x => CladdToModelName(x.className), x => x);
        _javaEntities = GeJavaParser.LoadExtraction("/tmp/datamodel/java_entities.yaml").classInfos
            .ToDictionary(x => CladdToModelName(x.className), x => x);

        // Just a bit lazy - did not make the model parsing recursive in ge_extract.
        Dictionary<string, ClassInfo> locationModels = GeJavaParser.LoadExtraction("/tmp/datamodel/java_model_location.yaml").classInfos
            .ToDictionary(x => CladdToModelName(x.className), x => x);
        _javaModels = new(_javaModels.Concat(locationModels));

        // Filter out anything that doesn't have the "@Data" annotation
        _javaModels = new(_javaModels.Where(x => x.Value.annotations.Any(x => x == "@Data")));

        // If a model does not have a corresponding Enity, use the model directly
        foreach (var kvp in _javaModels)
            if (!_javaEntities.ContainsKey(kvp.Key))
                _javaEntities[kvp.Key] = kvp.Value;

        foreach (var kvp in _javaModels) {
            string modelName = kvp.Key;
            ClassInfo jpModel = kvp.Value;
            ClassInfo jpEntity = _javaEntities[modelName];

            string group = FindGroup(modelName);

            Model model = new() {
                Name = modelName,
                QualifiedName = modelName,
                Description = jpEntity.javaDoc,
                Levels = [group],
            };

            SetModelProperties(jpEntity, model);
            CreateModelsForNestedChildren(jpModel, modelName, group);
            ConvertGetMethodsToAssociations(jpModel, modelName);

            _models.Add(model);
        }

        ColorModels();
    }

    private void ColorModels() {
        Dictionary<string, string> modelToParent = [];
        Dictionary<string, Model> nameToModel = _models.ToDictionary(x => x.QualifiedName, x => x);

        foreach (Association assoc in _associations)
            if (assoc.OwnerMultiplicity == Multiplicity.Aggregation)
                modelToParent[assoc.OtherSide] = assoc.OwnerSide;

        foreach (Model model in _models) {
            string rootModel = model.QualifiedName;
            while (true) {
                if (modelToParent.TryGetValue(rootModel, out string parentModelName))
                    rootModel = parentModelName;
                else
                    break;
            }

            model.Levels = [FindGroup(rootModel)];
        }
    }

    private void CreateModelsForNestedChildren(ClassInfo classInfo, string modelName, string group) {
        foreach (FieldInfo field in classInfo.privateFields) {
            if (IsPrimitive(field.type))
                continue;

            string childName = CladdToModelName(field.type);
            if (!_javaEntities.TryGetValue(childName, out ClassInfo childEntity)) {
                Console.WriteLine("WARNING: Property {0}.{1} does not have a matching Entity. Skipping.",
                    modelName, childName);
                continue;
            }


            // Only create a child model if it does not exist in _javaModels; otherwise, it would be created twice
            if (!_javaModels.ContainsKey(childName)) {
                Model childModel = new() {
                    Name = childName,
                    QualifiedName = childName,
                    Description = field.javaDoc,
                    Levels = [group],
                };

                SetModelProperties(childEntity, childModel);
                _models.Add(childModel);
            }

            // Add an Aggregation association to the child Model
            _associations.Add(new Association() {
                OwnerSide = modelName,
                OwnerMultiplicity = Multiplicity.Aggregation,
                OtherSide = childName,
                OtherMultiplicity = field.isArray ? Multiplicity.Many : Multiplicity.ZeroOrOne,
            });

        }
    }

    private static string GetMethodString(ClassInfo classInfo, MethodInfo method) {
        int paramCount = method.parameters.Count;
        string methodParams = string.Join(", ", method.parameters.Take(paramCount - 1).Select(x => x.name));
        return string.Format("{0} {1}.{2}({3}, ...)",
            method.returnType, classInfo.className, method.name, methodParams);
    }

    private void ConvertGetMethodsToAssociations(ClassInfo classInfo, string modelName) {
        foreach (MethodInfo method in classInfo.publicStaticMethods) {
            if (!method.name.StartsWith("get"))
                continue;

            int paramCount = method.parameters.Count;
            if (paramCount < 2) {
                string methodParams = string.Join(", ", method.parameters.Take(paramCount - 1).Select(x => x.name));
                Console.WriteLine("INFO: {0} has only {1} parameters. Skipping.",
                    GetMethodString(classInfo, method), paramCount);
                continue;
            }

            // The second-last parameter determines the association, if any
            ParameterInfo secondLastParam = method.parameters.AsEnumerable().Reverse().Skip(1).First();
            if (secondLastParam.type.ToLower() != "string") {
                Console.WriteLine("INFO: {0} - {1} is  of type {2}. Skipping.",
                    GetMethodString(classInfo, method), secondLastParam.name, secondLastParam.type);
                continue;
            }

            if (!KEY_TO_ENTITY.TryGetValue(secondLastParam.name.ToLower(), out string paramModelName)) {
                Console.WriteLine("INFO: {0} - no Entity found for {1}. Skipping.",
                    GetMethodString(classInfo, method), secondLastParam.name);
                continue;
            }

            string returnClass = ExtractReturnType(method.returnType, out bool isList);
            string returnModelName = CladdToModelName(returnClass);

            if (!_javaModels.ContainsKey(returnModelName)) {
                Console.WriteLine("INFO: {0} - return type {1} is not a known Model. Skipping.",
                    GetMethodString(classInfo, method), returnModelName);
                continue;
            }

            // methods like getXbyX-id() are assumed toexist and not helpful
            if (paramModelName == returnModelName)
                continue;

            // At this point, we have a complete mapping - create an aggregation
            Association association = association = new() {
                OwnerSide = paramModelName,
                OwnerMultiplicity = isList ? Multiplicity.One : Multiplicity.Many,
                OtherSide = returnModelName,
                OtherMultiplicity = isList ? Multiplicity.Many : Multiplicity.One,
            };

            association.Description = string.Format("Created from method: {0}",
                GetMethodString(classInfo, method));

            if (!string.IsNullOrEmpty(method.javaDoc))
                association.Description += "\n\n" + method.javaDoc;

            _associations.Add(association);
        }
    }

    private static string ExtractReturnType(string maybeList, out bool isList) {
        var m = Regex.Match(
            maybeList,
            @"^\s*List\s*<\s*([A-Za-z_]\w*)\s*>\s*$"
        );

        if (m.Success) {
            isList = true;
            return m.Groups[1].Value;
        }

        isList = false;
        return maybeList;
    }

    private string FindGroup(string modelName) {
        foreach (var kvp in GROUP_TO_MODELS)
            if (kvp.Value.Contains(modelName))
                return kvp.Key;
        return "unclassified";
    }

    private static void SetModelProperties(ClassInfo entity, Model model) {
        foreach (FieldInfo field in entity.privateFields) {
            string type = field.type;

            // This is a bit of a hack... The graphviz SVG tooltips do not do well if they have things with angled brackets
            // This should be handled in the SVG generator, not ad-hoc here.
            if (type.ToLower() == "list<string>")
                type = "String[]";

            Property property = new() {
                Name = field.name,
                Description = field.javaDoc,
                DataType = type,
                CanBeEmpty = true,
            };

            if (field.annotations.Count > 0)
                property.AddLabel("Annotations", string.Join("\n", field.annotations));

            model.AllProperties.Add(property);
        }
    }

    private static readonly HashSet<string> PRIMITIVE_TYPES = [
        "Boolean",
        "Double",
        "Float",
        "Instant",
        "Integer",
        "Long",
        "String",
    ];
    private static bool IsPrimitive(string type) {
        return PRIMITIVE_TYPES.Contains(type);
    }

    private static string CladdToModelName(string s) {
        return StripSuffix(StripSuffix(s, "Entity"), "Model");
    }

    private static string StripSuffix(string s, string suffix) {
        return s.EndsWith(suffix) ?
            s.Substring(0, s.Length - suffix.Length) : s;
    }

    public override IEnumerable<Parameter> GetParameters() {
        return [];
    }

    public override string GetTitle() {
        return "CI4Ops Data Fabric Schema - From Java";
    }

    public override IEnumerable<Model> GetModels() {
        return _models;
    }

    public override IEnumerable<Association> GetAssociations() {
        return _associations;
    }
    #endregion
}


using System.Collections.Generic;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace datamodel.schema.source.ge;

// To run this in Bartek's account:
// dotnet run ge dir=/Users/250023731/gitlab/docs-site/models/specification
public class GeJavaSource : SchemaSource {
    #region Members / Abstract 

    // Mapping of "targetField" key name to Entity name
    // Used to create associations from "Joins" where targetModel is same as the model 
    private readonly Dictionary<string, string> _keyToEntity = new() {
        ["patientid"] = "Patient",
        ["mrn"] = "Patient",
        ["patientmrn"] = "Patient",

        ["visitid"] = "PatientVisit",
        ["visitnumber"] = "PatientVisit",
        ["encounterid"] = "PatientVisit",
        ["patientcsn"] = "PatientVisit",
    };

    private readonly Dictionary<string, List<string>> _groupToModels = new() {
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
        _javaModels = GeJavaParser.LoadExtraction("/tmp/datamodel/java_models.yaml").classInfos
            .ToDictionary(x => StripSuffix(x.className, "Model"), x => x);
        _javaEntities = GeJavaParser.LoadExtraction("/tmp/datamodel/java_entities.yaml").classInfos
            .ToDictionary(x => StripSuffix(x.className, "Entity"), x => x);

        _javaModels.Remove("Base");

        foreach (var kvp in _javaModels) {
            string modelName = kvp.Key;
            if (!_javaEntities.TryGetValue(modelName, out ClassInfo entity)) {
                Console.WriteLine("WARNING: Model {0} does not have a matching Entity. Skipping.",
                    modelName);
                continue;
            }

            string group = FindGroup(modelName);

            Model model = new() {
                Name = modelName,
                QualifiedName = modelName,
                Description = entity.javaDoc,
                Levels = [group],
            };

            SetModelProperties(entity, model);
            CreateModelsForNestedChildren(kvp.Value, modelName, group);
            ConvertGetMethodsToAssociations(kvp.Value, modelName);

            _models.Add(model);
        }
    }

    private void CreateModelsForNestedChildren(ClassInfo classInfo, string modelName, string group) {
        foreach (FieldInfo field in classInfo.privateFields) {
            if (IsPrimitive(field.type))
                continue;

            string childName = StripSuffix(field.type, "Entity");
            if (!_javaEntities.TryGetValue(childName, out ClassInfo childEntity)) {
                Console.WriteLine("WARNING: Property {0}.{1} does not have a matching Entity. Skipping.",
                    modelName, childName);
                continue;
            }


            Model childModel = new() {
                Name = childName,
                QualifiedName = childName,
                Description = field.javaDoc,
                Levels = [group],
            };

            SetModelProperties(childEntity, childModel);
            _associations.Add(new Association() {
                OwnerSide = modelName,
                OwnerMultiplicity = Multiplicity.Aggregation,
                OtherSide = childName,
                OtherMultiplicity = field.isArray ? Multiplicity.Many : Multiplicity.ZeroOrOne,
            });

            _models.Add(childModel);
        }
    }

    private void ConvertGetMethodsToAssociations(ClassInfo classInfo, string modelName) {
        foreach (MethodInfo method in classInfo.publicStaticMethods) {
            if (!method.name.StartsWith("get"))
                continue;

            int paramCount = method.parameters.Count;
            if (paramCount != 2) {
                Console.WriteLine("INFO: {0}.{1}() has {2} parameters. Skipping for now.",
                    classInfo.className, method.name, paramCount);
                continue;
            }

            ParameterInfo firstParam = method.parameters.First();
            if (firstParam.type.ToLower() != "string") {
                Console.WriteLine("INFO: {0}.{1}() first parameter is {2}. Skipping for now.",
                    classInfo.className, method.name, firstParam.type);
                continue;
            }

            if (!_keyToEntity.TryGetValue(firstParam.name.ToLower(), out string paramModelName)) {
                Console.WriteLine("INFO: {0}.{1}() first parameter {2} - no entity found. Skipping for now.",
                    classInfo.className, method.name, firstParam.name);
                continue;
            }

            string returnClass = ExtractReturnType(method.returnType, out bool isList);
            string returnModelName = StripSuffix(returnClass, "Model");

            if (!_javaModels.ContainsKey(returnModelName)) {
                Console.WriteLine("INFO: {0}.{1}() return type {2} is not a known model. Skipping for now.",
                    classInfo.className, method.name, returnModelName);
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

            association.Description = string.Format("Created from method: {0} {1}.{2}({3}, ...)",
                method.returnType, classInfo.className, method.name, firstParam.name);

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
        foreach (var kvp in _groupToModels)
            if (kvp.Value.Contains(modelName))
                return kvp.Key;
        return "unclassified";
    }

    private static void SetModelProperties(ClassInfo entity, Model model) {
        foreach (FieldInfo field in entity.privateFields) {
            string type = field.type;
            if (type.ToLower() == "list<string>")
                type = "String[]";

            Property property = new() {
                Name = field.name,
                Description = field.javaDoc,
                DataType = type,
                CanBeEmpty = true,
            };

            if (field.annotations.Count > 0)
                property.AddLabel("Annotations", string.Join(", ", field.annotations));

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


using System.Collections.Generic;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace datamodel.schema.source.ge;

// To run this in Bartek's account:
// dotnet run ge dir=/Users/250023731/gitlab/docs-site/models/specification
public class GeJavaSource : SchemaSource {
    #region Members / Abstract 

    // Mapping of "targetField" key name to Entity name
    // Used to create associations from "Joins" where targetModel is same as the model 
    private readonly Dictionary<string, string> _keyToEntity = new() {
        ["patientId"] = "Patient",
        ["mrn"] = "Patient",
        ["patientMRN"] = "Patient",
        ["visitID"] = "PatientVisit",
        ["visitNumber"] = "PatientVisit",
        ["encounterId"] = "PatientVisit",
        ["patientCSN"] = "PatientVisit",
    };

    private readonly Dictionary<string, List<string>> _groupToModels = new() {
        ["appointment"] = ["Appointment"],
        ["patient-visit"] = ["AvoidableNights", "DepartureLounge", "Device", "DischargeMilestones", "EmrFlags", "InfectiousDisease", "Patient", "PatientVisit", "PostAcute"],
        ["transfer"] = ["BedRequest", "TransferRequest", "TransportExternal", "TransportInternal"],
        ["miscellaneous"] = ["Equipment", "FlowSheet", "PressureForecast", "PliEditRequest"],
        ["personnel"] = ["HospitalPersonnel", "HospitalPersonnelSchedule", "HospitalPersonnelTimesheet"],
        ["location"] = ["InternalLocationMaster", "PressureScore", "DivertStatus", "BedsLocationMaster", "FacilitiesLocationMaster", "UnitsLocationMaster"],
        ["order"] = ["Order"],
    };

    private Dictionary<string, ClassInfo> _javaModels = [];
    private Dictionary<string, ClassInfo> _javaEntities = [];
    private readonly List<Model> _models = [];
    private readonly List<Association> _associations = [];
    private string _currentFilename = "";

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

            // TODO
            string group = FindGroup(modelName);

            Model model = new() {
                Name = modelName,
                QualifiedName = modelName,
                Description = entity.javaDoc,
                Levels = [group],
            };

            SetModelProperties(entity, model);

            // Created models for nested children with aggregation associations
            foreach (FieldInfo field in kvp.Value.privateFields) {
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

            _models.Add(model);
        }
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
            model.AllProperties.Add(new() {
                Name = field.name,
                Description = field.javaDoc,
                DataType = type,
                CanBeEmpty = true,
            });
        }
    }

    private static HashSet<string> PRIMITIVE_TYPES = [
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


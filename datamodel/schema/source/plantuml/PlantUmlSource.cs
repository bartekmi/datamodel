using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace datamodel.schema.source.plantuml;

public class PlantUmlSource : SchemaSource {
  private const string PARAM_FILE = "file";

  private readonly List<Model> _models = [];
  private readonly List<Association> _associations = [];

  public override void Initialize(Parameters parameters) {
    string[] lines = parameters.GetFileContent(PARAM_FILE).Split("\n");
    Parse(lines);
  }

  public override IEnumerable<Parameter> GetParameters() {
    return [
        new() {
                Name = PARAM_FILE,
                Description = "The name of the file which contains the PlantUML model",
                Type = ParamType.File,
                IsMandatory = true,
            }
    ];
  }

  public override string GetTitle() => "PlantUML Model";

  public override IEnumerable<Model> GetModels() => _models;

  public override IEnumerable<Association> GetAssociations() => _associations;

  private void Parse(string[] lines) {
    Dictionary<string, Model> modelMap = [];
    Model currentModel = null;

    foreach (string rawLine in lines) {
      string line = rawLine.Trim();

      if (string.IsNullOrWhiteSpace(line) ||
          line.StartsWith("@") ||     // @startuml, @enduml
          line.StartsWith("'"))       // Comment
        continue;

      // class declaration. Matches...
      //   class ClassName
      //   class ClassName {
      Match classDecl = Regex.Match(line, @"^class\s+(\w+)\s*(\{)?");
      if (classDecl.Success) {
        string className = classDecl.Groups[1].Value;
        currentModel = new Model {
          Name = className,
          QualifiedName = className,
          AllProperties = []
        };
        _models.Add(currentModel);
        modelMap[className] = currentModel;
        continue;
      }

      // end of class body
      if (line == "}" && currentModel != null) {
        currentModel = null;
        continue;
      }

      // field inside class. Matches...
      //   Type Name
      //   ~ Type Name 
      if (currentModel != null) {
        Match fieldDecl = Regex.Match(line, @"^([+#\-~])?\s*(\w+)\s+(\w+)");
        if (fieldDecl.Success) {
          string type = fieldDecl.Groups[2].Value;
          string name = fieldDecl.Groups[3].Value;

          currentModel.AllProperties.Add(new Property {
            Name = name,
            DataType = type
          });
        }
        continue;
      }

      // inheritance. Maatches...
      //  Employee --|> Person
      Match inheritance = Regex.Match(line, @"^(\w+)\s+--\|>\s+(\w+)");
      if (inheritance.Success) {
        string subClass = inheritance.Groups[1].Value;
        string superClass = inheritance.Groups[2].Value;
        if (modelMap.TryGetValue(subClass, out var subModel))
          subModel.SuperClassName = superClass;
        continue;
      }

      // Association. Matches...
      //  Person "1" --> "0..*" Address : livesAt
      //  Order "1" o-- "0..*" Item : contains
      //  House "1" *-- "1..3" Room
      Match association = Regex.Match(line, @"^(\w+)\s+""([^""]+)""\s+([<o*]*[-.]+[->o]*)\s+""([^""]+)""\s+(\w+)(\s*:\s*(.+))?");
      if (association.Success) {
        string aModel = association.Groups[1].Value;
        string aCard = association.Groups[2].Value;
        string arrow = association.Groups[3].Value;
        string bCard = association.Groups[4].Value;
        string bModel = association.Groups[5].Value;
        string role = association.Groups[7].Success ? association.Groups[7].Value : null;

        Association assoc = new() {
          OwnerSide = aModel,
          OwnerMultiplicity = ParseMultiplicity(aCard),
          OtherSide = bModel,
          OtherMultiplicity = ParseMultiplicity(bCard),
          OtherRole = role
        };

        _associations.Add(assoc);
        continue;
      }
    }
  }

  private Multiplicity ParseMultiplicity(string card) {
    return card switch {
      "1" => Multiplicity.One,
      "0..1" => Multiplicity.ZeroOrOne,
      "0..*" => Multiplicity.Many,
      "*" => Multiplicity.Many,
      "Many" => Multiplicity.Many,
      "Aggregation" => Multiplicity.Aggregation,
      _ => Multiplicity.One
    };
  }
}

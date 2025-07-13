using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core.Events;

namespace datamodel.schema.source.plantuml;

public class PlantUmlSource : SchemaSource {
  private const string PARAM_FILE = "file";

  private readonly Dictionary<string, Model> _models = [];
  private readonly List<Association> _associations = [];

  public override void Initialize(Parameters parameters) {
    string[] lines = parameters.GetFileContent(PARAM_FILE).Split("\n");
    Dictionary<string, string> subclassToSuperclass = Parse(lines);

    foreach (var mapping in subclassToSuperclass)
      if (_models.TryGetValue(mapping.Key, out Model model))
        model.SuperClassName = mapping.Value;
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

  public override IEnumerable<Model> GetModels() => _models.Values;

  public override IEnumerable<Association> GetAssociations() => _associations;

  private Dictionary<string, string> Parse(string[] lines) {
    Model currentModel = null;
    Dictionary<string, string> subclassToSuperclass = [];

    foreach (string line in GetLogicalLines(lines)) {
      if (line == "}") {
        currentModel = null;
        getAndClearComments();    // Prevent stray comments from leaking into next definition
        continue;
      }

      if (currentModel != null) {
        // field inside class. Matches...
        //   Type Name
        //   ~ Type Name
        Match fieldDecl = Regex.Match(line, @"^([+#\-~])?\s*(\w+)\s+(\w+)");
        if (fieldDecl.Success) {
          string type = fieldDecl.Groups[2].Value;
          string name = fieldDecl.Groups[3].Value;

          currentModel.AllProperties.Add(new Property {
            Name = name,
            DataType = type,
            Description = getAndClearComments(),
          });
        }
        continue;
      }

      // class declaration. Matches...
      //   class ClassName
      //   class ClassName {
      Match classDecl = Regex.Match(line, @"^class\s+(\w+)\s*(\{)?");
      if (classDecl.Success) {
        string className = classDecl.Groups[1].Value;
        currentModel = new Model {
          Name = className,
          QualifiedName = className,
          Description = getAndClearComments(),
          AllProperties = []
        };
        _models[className] = currentModel;

        // For the case without a terminal {, there are no properties
        if (!line.EndsWith('{'))
          currentModel = null;
      }

      // inheritance. Maatches...
      //  Employee --|> Person
      Match inheritance = Regex.Match(line, @"^(\w+)\s+--\|>\s+(\w+)");
      if (inheritance.Success) {
        string subClass = inheritance.Groups[1].Value;
        string superClass = inheritance.Groups[2].Value;
        subclassToSuperclass[subClass] = superClass;
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
          OtherRole = role,
          Description = getAndClearComments(),
        };

        if (arrow == "o--")
          assoc.OwnerMultiplicity = Multiplicity.Aggregation;

        _associations.Add(assoc);
      }
    }

    return subclassToSuperclass;
  }

  private readonly StringBuilder _commentsBuilder = new();
  private string getAndClearComments() {
    if (_commentsBuilder.Length == 0)
      return null;

    string comments = _commentsBuilder.ToString().Trim();
    _commentsBuilder.Clear();
    return comments;
  }

  private IEnumerable<string> GetLogicalLines(string[] lines) {
    foreach (string rawLine in lines) {
      string line = rawLine.Trim();

      if (line.StartsWith("'")) {
        _commentsBuilder.Append(line[1..].Trim());
        _commentsBuilder.AppendLine();
        continue;
      }

      if (string.IsNullOrWhiteSpace(line) || line.StartsWith("@"))
        continue;

      yield return line;
    }
  }

  private Multiplicity ParseMultiplicity(string card) {
    return card switch {
      "1" => Multiplicity.One,
      "0..1" => Multiplicity.ZeroOrOne,
      "0..*" => Multiplicity.Many,
      "1..*" => Multiplicity.Many,  // Future: we could have new type for this
      "*" => Multiplicity.Many,
      _ => Multiplicity.One
    };
  }
}

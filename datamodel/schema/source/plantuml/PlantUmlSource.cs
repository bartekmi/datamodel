using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace datamodel.schema.source.plantuml;

// Parser for PlantUML language for Data Models.
// Online display of raw PlanUML: https://www.plantuml.com
public class PlantUmlSource : SchemaSource {
  private const string PARAM_PATHS = "paths";

  private readonly Dictionary<string, Model> _models = [];
  private readonly List<Association> _associations = [];

  public override void Initialize(Parameters parameters) {
    FileOrDir[] fileOrDirs = parameters.GetFileOrDirs(PARAM_PATHS);
    IEnumerable<PathAndContent> files = FileOrDir.Combine(fileOrDirs);
    Dictionary<string, string> subclassToSuperclass = [];

    foreach (PathAndContent pac in files) {
      string[] lines = pac.Content.Split("\n");
      Parse(subclassToSuperclass, lines);
    }

    foreach (var mapping in subclassToSuperclass)
      if (_models.TryGetValue(mapping.Key, out Model model))
        model.SuperClassName = mapping.Value;
  }

  public override IEnumerable<Parameter> GetParameters() {
    return [
      new ParameterFileOrDir() {
          Name = PARAM_PATHS,
          Description = "The name of the file or directoery which contains the root *.puml file(s). If directory, it is scanned recursively.",
          IsMandatory = true,
          IsMultiple = true,
          FilePattern = "*.puml",
          ReadContent = true,
      },
    ];
  }

  public override string GetTitle() => "PlantUML Model";

  public override IEnumerable<Model> GetModels() => _models.Values;

  public override IEnumerable<Association> GetAssociations() => _associations;

  private void Parse(Dictionary<string, string> subclassToSuperclass, string[] lines) {
    Model currentModel = null;

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
      //.  class namespece.ClassName
      //   class ClassName
      //   class ClassName {
      Match classDecl = Regex.Match(line, @"^class\s+([\w._]+)\s*(\{)?");
      if (classDecl.Success) {
        string qualClassName = classDecl.Groups[1].Value;
        string[] pieces = qualClassName.Split('.');
        string className = pieces.Last();
        string[] levels = pieces.Take(pieces.Count() - 1).ToArray();

        currentModel = new Model {
          Name = className,
          QualifiedName = qualClassName,
          Levels = levels,
          Description = getAndClearComments(),
          AllProperties = []
        };
        _models[qualClassName] = currentModel;

        // For the case without a terminal {, there are no properties
        if (!line.EndsWith('{'))
          currentModel = null;

        continue;
      }

      // inheritance. Maatches...
      //  Employee --|> Person
      Match inheritance = Regex.Match(line, @"^([\w._]+)\s+--\|>\s+([\w._]+)");
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
      Match association = Regex.Match(line, @"^([\w._]+)\s+""([^""]+)""\s+([<o*]*[-.]+[->o]*)\s+""([^""]+)""\s+([\w._]+)(\s*:\s*(.+))?");
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
        continue;
      }

      // If we got this far, we did not understand the line. Output an error
      Error.Log("Did not understand PlantUML line: {0}", line);
    }
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

      if (line.StartsWith('@')) {
        getAndClearComments();  // This allows for comments before @startuml
        continue;
      }

      if (string.IsNullOrWhiteSpace(line))  // Ignore blank lines
        continue;

      yield return line;
    }
  }

  private static Multiplicity ParseMultiplicity(string card) {
    if (int.TryParse(card, out int value) && value > 1)
      return Multiplicity.Many;

    switch (card) {
      case "1":
        return Multiplicity.One;
      case "0..1":
        return Multiplicity.ZeroOrOne;
      case "0..*":
      case "1..*":
      case "*":
        return Multiplicity.Many;
      default:
        Error.Log("Unexpected multiplicity: {0}", card);
        return Multiplicity.One;  // Fallback
    }
    ;
  }
}

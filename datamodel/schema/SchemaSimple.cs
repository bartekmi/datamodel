using System;
using System.Collections.Generic;
using System.Linq;

namespace datamodel.schema;

public class SchemaSimple {
  public const string LABEL_RETURNED_FOR_OPERATION = "Retruned for Operation";

  public Dictionary<string, SSModel> Entities = [];
  public Dictionary<string, SSModelInfo> TopLevelEntities = [];

  internal static SchemaSimple From(Schema schema) {
    SchemaSimple ss = new();
    foreach (Model model in schema.Models) {
      ss.Entities[model.QualifiedName] = SSModel.From(model);

      IEnumerable<Label> forOperations = model.FindLabels(LABEL_RETURNED_FOR_OPERATION);
      if (forOperations.Any())
        ss.TopLevelEntities[model.QualifiedName] = SSModelInfo.From(model, forOperations);
    }

    return ss;
  }
}

public class SSModelInfo {
  public string Documentation;
  public string[] ReturnedForOperations;

  internal static SSModelInfo From(Model model, IEnumerable<Label> forOperations) {
    return new() {
      Documentation = model.Description,
      ReturnedForOperations = forOperations.Select(x => x.Value).ToArray(),
    };
  }
}

public class SSModel {
  public string Documentation;
  public Dictionary<string, SSProperty> Fields = [];

  internal static SSModel From(Model model) {
    SSModel ssModel = new() {
      Documentation = model.Description,
    };

    foreach (Property prop in model.AllProperties)
      if (!prop.IsRef)
        ssModel.Fields[prop.Name] = SSProperty.From(prop);

    return ssModel;
  }
}

public class SSProperty {
  public string Type;
  public string Documentation;

  internal static SSProperty From(Property prop) {
    return new() {
      Type = prop.DataType,
      Documentation = prop.Description,
    };
  }
}
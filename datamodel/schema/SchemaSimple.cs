using System.Collections.Generic;

namespace datamodel.schema;

public class SchemaSimple {
  public Dictionary<string, SSModel> Entities = [];

  internal static SchemaSimple From(Schema schema) {
    SchemaSimple ss = new();
    foreach (Model model in schema.Models)
      ss.Entities[model.Name] = SSModel.From(model);

    return ss;
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
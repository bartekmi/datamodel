using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using datamodel.schema.tweaks;
using datamodel.utils;

namespace datamodel.schema.source.botocore;

internal class RemoveLowValuesModels : FilterModelsTweak {
  private readonly HashSet<string> LOW_VALUE_MODEL_NAMES = ["Tag", "AnalysisComponent"];

  public override IEnumerable<Model> ModelsToFilterOut(TempSource source) {
    return source.Models.Values
      .Where(x => LOW_VALUE_MODEL_NAMES.Contains(x.Name));
  }
}

internal class RenameModels : Tweak {
  internal RenameModels() : base(TweakApplyStep.PreHydrate) { }

  public override void Apply(TempSource source) {
    foreach (var item in source.Models.ToList()) {
      string newName = NewName(item.Value.Name);
      if (newName != null)
        source.RenameModel(item.Value, newName);
    }
  }

  private string NewName(string originalName) {
    if (originalName.EndsWith("ListItem")) {
      string coreName = originalName.Substring(0, originalName.Length - "ListItem".Length);
      return NameUtils.Singluarize(coreName);
    }

    if (originalName.StartsWith("Get") && originalName.EndsWith("Response"))
      return originalName.Substring("Get".Length, originalName.Length - "Get".Length - "Response".Length);

    return null;  // Means no change is needed
  }
}


internal class ReadAiAssociationFiles : Tweak {
  private const string AI_DATA_DIR = "schema/source/botocore/ai_data/chatgpt";

  internal ReadAiAssociationFiles() : base(TweakApplyStep.PreHydrate) { }

  public override void Apply(TempSource source) {
    string[] files = Directory.GetFiles(AI_DATA_DIR, "*.toplevel.json", SearchOption.TopDirectoryOnly);

    foreach (string filePath in files) {
      string json = File.ReadAllText(filePath);
      List<EntityInfo> entityInfos = JsonUtils.Deserialize<List<EntityInfo>>(json);

      foreach (EntityInfo enityInfo in entityInfos.Where(x => !x.TopLevel))
        source.Associations.Add(new Association() {
          OwnerSide = enityInfo.OwnedBy,
          OwnerMultiplicity = Multiplicity.Aggregation,
          OtherSide = enityInfo.Entity,
          OtherMultiplicity = TranslateMultiplicity(enityInfo.Entity, enityInfo.ChildEntityCardinality),
        });
    }
  }

  private Multiplicity TranslateMultiplicity(string entity, string cardinality) {
    switch (cardinality) {
      case "one": return Multiplicity.One;
      case "zero-or-one": return Multiplicity.ZeroOrOne;
      case "many": return Multiplicity.Many;
      default:
        throw new Exception(string.Format("Unexpected cardinality {0} in Entity {1}", cardinality, entity));
    }
  }

  internal class EntityInfo {
    public string Entity;
    public bool TopLevel;
    public string OwnedBy;
    public string ChildEntityCardinality;
  }
}




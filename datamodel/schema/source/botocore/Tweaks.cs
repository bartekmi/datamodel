using System.Collections.Generic;
using System.Linq;
using datamodel.schema.tweaks;

namespace datamodel.schema.source.botocore;

internal class RemoveLowValuesModels : FilterModelsTweak {
  private HashSet<string> LOW_VALUE_MODEL_NAMES = ["Tag", "AnalysisComponent"];

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
    if (originalName.EndsWith("ListItem"))
      return originalName.Substring(0, originalName.Length - "ListItem".Length);

    if (originalName.StartsWith("Get") && originalName.EndsWith("Response"))
      return originalName.Substring("Get".Length, originalName.Length - "Get".Length - "Response".Length);

    return null;  // Means no change is needed
  }
}

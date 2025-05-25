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

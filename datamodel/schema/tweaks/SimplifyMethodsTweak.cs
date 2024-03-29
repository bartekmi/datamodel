using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.source;

namespace datamodel.schema.tweaks {
    // It is a frequesnt pattern (e.g. protobuf) that the sole purpose of a model
    // is as a holding tank for either input or output parameters of a Method.
    // If such a model holds only scalars, and especially if the number of the 
    // properties of such a model is low, the diagrams can be simplified by enumerating
    // the parameters right in the method, rather than showing those "holding tank"
    // models.
    //
    // This "tweak" does the following:
    // - Examines all Methods of all Models
    // - If a Method meetigs the following conditions (done separately for inputs and outputs)
    //   - Input/output consists of a single model
    //   - That model is used nowhere else [Skipping for now]
    //   - The model consists only of scalar Properties
    //   - The number of scalar properties is <= MaxNumberOfProperties
    // 
    // - Then the following modification is done...
    //   - The "holding tank" model is removed
    //   - The input/output argument list of the Method is converted to the corresponding
    //     list of scalar Properties.
    public class SimplifyMethodsTweak : Tweak {
        public int MaxNumberOfProperties = 4;

        public SimplifyMethodsTweak() : base(TweakApplyStep.PreHydrate) {}

        public override void Apply(TempSource source) {
            HashSet<string> modelsWithOutgoing = 
                new HashSet<string>(source.Associations.Select(x => x.OwnerSide));

            foreach (Model model in source.GetModels()) {
                foreach (Method method in model.Methods) {
                    // Possibly tweak the inputs
                    List<NamedType> newInputs = MaybeDoTweak(source, modelsWithOutgoing, method.Inputs);
                    if (newInputs != null)
                        method.Inputs = newInputs;

                    // Possibly tweak the outputs
                    List<NamedType> newOutputs = MaybeDoTweak(source, modelsWithOutgoing, method.Outputs);
                    if (newOutputs != null)
                        method.Outputs = newOutputs;
                }
            }
        }

        private List<NamedType> MaybeDoTweak(
            TempSource source, 
            HashSet<string> modelsWithOutgoing, 
            List<NamedType> existing) {

            // "Single"?
            if (existing.Count != 1)
                return null;

            // "Model"?
            string qualifiedName = existing.Single().Type.Name;
            Model model = source.FindModel(qualifiedName);
            if (model == null)
                return null;

            // No outgoing associations?
            if (modelsWithOutgoing.Contains(model.QualifiedName))
                return null;

            if (model.AllProperties.Count > MaxNumberOfProperties)
                return null;

            // At This point, we know we *Should* do the tweak...
            source.RemoveModel(model);

            // Create list of types to substitute in the Method
            List<NamedType> newTypes = new List<NamedType>();
            foreach (Property prop in model.AllProperties)
                newTypes.Add(new NamedType() {
                    Name = prop.Name,
                    Type = prop.DataTypeObj,
                });

            return newTypes;
        }
    }
}
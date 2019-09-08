using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using YamlDotNet.RepresentationModel;

using datamodel.utils;
using datamodel.schema;

namespace datamodel.metadata {
    public static class YamlVisualizationsParser {

        private const string MODELS_YAML_FILENAME = "visualizations.yaml";

        public static bool ExistsInThisDirectory(string dir) {
            return File.Exists(Path.Combine(dir, MODELS_YAML_FILENAME));
        }

        public static IEnumerable<GraphDefinition> Parse(string dir) {
            string path = Path.Combine(dir, MODELS_YAML_FILENAME);
            YamlSequenceNode root = (YamlSequenceNode)YamlUtils.ReadYaml(path).RootNode;
            return root.Select(x => ParseGraphDefinitioin((YamlMappingNode)x));
        }

        private static GraphDefinition ParseGraphDefinitioin(YamlMappingNode yamlGraphDef) {
            string[] coreModels = YamlUtils.GetCommaSeparatedString(yamlGraphDef, "coreModels");
            string[] extraModels = YamlUtils.GetCommaSeparatedString(yamlGraphDef, "extraModels");

            return new GraphDefinition {
                Style = RenderingStyle.Dot,     // TODO
                CoreModels = ToModelsWithValidation(coreModels),
                ExtraModels = ToModelsWithValidation(extraModels),
                NameComponents = YamlUtils.GetCommaSeparatedString(yamlGraphDef, "nameComponents"),
            };
        }

        private static Model[] ToModelsWithValidation(string[] modelNames) {
            List<Model> models = new List<Model>();
            foreach (string modelName in modelNames) {
                Model model = Schema.Singleton.FindByClassName(modelName);
                if (model == null)
                    Error.Log("Model name does not exist: " + modelName);
                else
                    models.Add(model);
            }
            return models.ToArray();
        }
    }
}
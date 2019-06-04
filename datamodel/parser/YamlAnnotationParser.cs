using System.IO;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using datamodel.schema;
using datamodel.utils;

namespace datamodel.parser {
    public static class YamlAnnotationParser {
        public static void Parse(Table table, List<Error> errors) {
            string path = table.AnnotationFilePath;
            YamlMappingNode root = (YamlMappingNode)YamlUtils.ReadYaml(path).RootNode;
            SetCommonElements(table, root);

            ParseColumns(root, "columns", table, errors, path);
            ParseColumns(root, "foreignKeyColumns", table, errors, path);
        }

        private static void ParseColumns(YamlMappingNode root, string key, Table table, List<Error> errors, string path) {
            YamlSequenceNode items = YamlUtils.GetSequence(root, key);
            foreach (YamlMappingNode item in items) {
                string columnName = YamlUtils.GetString(item, "name");
                Column column = table.FindColumn(columnName);
                if (column == null)
                    errors.Add(new Error() {
                        Path = path,
                        Message = string.Format("Column name {0}.{1} mentioned in this file is not found in the Schema", table.DbName, columnName)
                    });
                else
                    SetCommonElements(column, item);
            }
        }

        private static void SetCommonElements(IDbElement element, YamlMappingNode node) {
            element.Description = YamlUtils.GetString(node, "description");
            element.Group = YamlUtils.GetString(node, "group");
        }
    }
}
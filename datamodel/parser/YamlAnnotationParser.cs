using System.IO;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using datamodel.schema;
using datamodel.utils;

namespace datamodel.parser {
    public static class YamlAnnotationParser {
        public static void Parse(Table table) {
            string path = table.AnnotationFilePath;
            if (!File.Exists(path)) {
                Error.Log("Warning: No YAML annotation file: " + path);
                return;
            }

            YamlMappingNode root = (YamlMappingNode)YamlUtils.ReadYaml(path).RootNode;
            SetCommonElements(table, root);

            ParseColumns(root, "columns", table, path);
            ParseColumns(root, "foreignKeyColumns", table, path);
        }

        private static void ParseColumns(YamlMappingNode root, string key, Table table, string path) {
            YamlSequenceNode items = YamlUtils.GetSequence(root, key);
            if (items == null)
                return;         // Table might not have any primary or FK columns

            foreach (YamlMappingNode item in items) {
                string columnName = YamlUtils.GetString(item, "name");
                Column column = table.FindColumn(columnName);
                if (column == null)
                    Error.Log(new Error() {
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
            element.Deprecated = YamlUtils.GetBoolean(node, "deprecated");
        }
    }
}
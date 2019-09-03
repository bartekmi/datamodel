using System;
using System.IO;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace datamodel.utils {
    public static class YamlUtils {
        public static YamlDocument ReadYaml(string path) {
            using (StreamReader reader = new StreamReader(new FileStream(path, FileMode.Open))) {
                YamlStream yaml = new YamlStream();
                yaml.Load(reader);
                YamlDocument document = yaml.Documents[0];
                return document;
            }
        }

        public static YamlSequenceNode GetSequence(YamlMappingNode node, string key) {
            return GetChild(node, key) as YamlSequenceNode;
        }

        public static YamlNode GetChild(YamlMappingNode node, string key) {
            if (node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode value))
                return value;
            return null;
        }

        public static string GetString(YamlMappingNode node, string key, bool exceptionIfMissing = false) {
            YamlNode child = GetChild(node, key);

            if (child == null && exceptionIfMissing)
                throw new Exception("Key not found: " + key);

            return child == null ? null : child.ToString();
        }

        public static bool GetBoolean(YamlMappingNode node, string key) {
            string value = GetString(node, key, false);
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return bool.Parse(value);
        }

        public static int GetInt(YamlMappingNode node, string key) {
            string value = GetString(node, key, false);
            return int.Parse(value);
        }
    }
}
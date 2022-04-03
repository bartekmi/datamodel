using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace datamodel.schema.source.from_data {

    // In some cases, rather than an object having ordinary properties, they key of the object actually
    // constitutes data. Kubernetes Swagger has lots of this.
    internal static class SampleDataKeyIsData {
        internal const string ROOT_PATH = "";
        private const string KEY_COLUMN = "__key__";

        internal static void ConvertObjectsWhereKeyIsData(
            SampleDataSchemaSource.Options options,
            SDSS_Element root
            ) {

            HashSet<string> paths = new HashSet<string>(options.PathsWhereKeyIsData);
            RecursePass1(options, paths, root, ROOT_PATH);
            RecursePass2(paths, root, ROOT_PATH, true);
        }

        #region Pass 1 - Identify all paths where the object uses keys as data
        private static void RecursePass1(
            SampleDataSchemaSource.Options options,
            HashSet<string> paths, 
            SDSS_Element element, 
            string path) {

            if (element.IsPrimitive)
                return;

            if (element.IsArray) {
                foreach (SDSS_Element child in element.ArrayItems)
                    RecursePass1(options, paths, child, path);
            } else if (element.IsObject) {

                bool isKeyData = paths.Contains(path);
                if (!isKeyData) {
                    isKeyData = IsKeyData(options, element);
                    if (isKeyData)
                        paths.Add(path);
                }

                foreach (KeyValuePair<string, SDSS_Element> pair in element.ObjectItems) {
                    string childPath = isKeyData ? path : AppendToPath(path, pair.Key);
                    RecursePass1(options, paths, pair.Value, childPath);
                }
            } else
                throw new Exception("Added new type, forgot to change code?");
        }

        // TODO: Move to options
        private static bool IsKeyData(SampleDataSchemaSource.Options options, SDSS_Element obj) {
            if (obj.ObjectItems.Count() > 50 ||       // TODO: Obviusly, this should be moved to options
                obj.ObjectItems.Keys.Any(x => !options.KeyIsDataRegex.IsMatch(x)))
                return true;

            return false;
        }

        #endregion

        #region Pass 2 - Convert all objects where key is used as to arrays
        private static void RecursePass2(HashSet<string> paths, SDSS_Element element, string path, bool checkKID) {
            if (element.IsPrimitive)
                return;

            // This is the magic momemnt... If we are dealing with an object which should
            // really be an array - convert it. then, the IsArray branch will take care of the rest
            if (checkKID && element.IsObject && paths.Contains(path))
                element.ConvertObjectToArray(KEY_COLUMN);

            if (element.IsArray) {
                foreach (SDSS_Element child in element.ArrayItems)
                    RecursePass2(paths, child, path, false);
            } else if (element.IsObject)
                foreach (KeyValuePair<string, SDSS_Element> pair in element.ObjectItems) {
                    string childPath = AppendToPath(path, pair.Key);
                    RecursePass2(paths, pair.Value, childPath, true);
                }
            else
                throw new Exception("Added new type, forgot to change code?");
        }
        #endregion

        #region Utilities
        internal static string AppendToPath(string path, string next) {
            return string.Format("{0}.{1}", path, next);
        }

        internal static void MaybeAddKeyColumn(Model model, string example) {
            if (model.FindColumn(KEY_COLUMN) != null)
                return;

            Column column = new Column() {
                Name = KEY_COLUMN,
                DataType = "String",
                CanBeEmpty = false,
                Owner = model,
            };
            model.AllColumns.Insert(0, column);

            if (example != null)
                column.AddLabel("Example", example);
        }
        #endregion
    }
}

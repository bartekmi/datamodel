using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// This class takes care of imports in protobuf.

namespace datamodel.schema.source.protobuf {
    public class ProtobufImporter {

        public FileBundle ReadDirTree(string paths) {
            throw new NotImplementedException();
        }

        public FileBundle ReadFiles(string[] paths) {
            FileBundle bundle = new FileBundle();

            foreach (string path in paths)
                ReadFile(bundle, path, null);

            return bundle;
        }

        private void ReadFile(FileBundle bundle, string path, string[] typesOfInterest) {
            // TODO... This is too simplistic, because we may have different typesOfInterest
            // if (bundle.HasFile(path))
            //     return;

            string fileData = System.IO.File.ReadAllText(path);
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(fileData));
            ProtobufParser parser = new ProtobufParser(tokenizer);
            File file = parser.Parse();
            file.Path = path;

            bundle.AddFile(file);

            foreach (Import import in file.Imports) {
                string[] importTypesOfInterest = GetImportedTypes(file);
                ReadFile(bundle, import.ImportPath, importTypesOfInterest);
            }
        }

        private string[] GetImportedTypes(File file) {
            List<Type> types = new List<Type>();

            foreach (Message message in file.Messages)
                AddTypesForMessage(types, message);
            foreach (Extend extend in file.Extends)
                AddTypesForExtend(types, extend);

            return types
                .Where(x => x.IsImported)
                .Select(x => x.Name)
                .Distinct()
                .ToArray();
        }

        private void AddTypesForMessage(List<Type> types, Message message) {
            foreach (Field field in message.Fields)
                types.AddRange(field.UsedTypes());

            foreach (Field field in message.Extends.SelectMany(x => x.Fields))
                types.AddRange(field.UsedTypes());

            foreach (Message nested in message.Messages)
                AddTypesForMessage(types, nested);

            foreach (Extend extend in message.Extends)
                AddTypesForExtend(types, extend);
        }

        private void AddTypesForExtend(List<Type> types, Extend extend) {
            foreach (Field field in extend.Fields)
                types.AddRange(field.UsedTypes());

            AddTypesForMessage(types, extend.Message);
        }

    }

    #region Helper Classes
    public class FileBundle {
        // Key: path, Value: parsed File
        private Dictionary<string, File> _fileDict = new Dictionary<string, File>();
        private Dictionary<string, List<File>> _packageDict = new Dictionary<string, List<File>>();

        public bool HasFile(string path) {
            return _fileDict.ContainsKey(path);
        }

        public void AddFile(File file) {
            // Remember path => file
            string path = file.Path;
            if (_fileDict.ContainsKey(path))
                throw new Exception("Should never happen - trying to add path again: " + path);
            _fileDict[path] = file;

            // Remember package => list of files
            string package = file.Package;
            if (!_packageDict.TryGetValue(package, out List<File> files)) {
                files = new List<File>();
                _packageDict[package] = files;
            }
            files.Add(file);
        }
        #endregion
    }
}
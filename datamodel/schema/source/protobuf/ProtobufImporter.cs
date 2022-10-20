using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;

using Newtonsoft.Json;
// This class takes care of imports in protobuf.

namespace datamodel.schema.source.protobuf {
    public class ProtobufImporter {
        private string _importBasePath;

        public ProtobufImporter(string importBasePath) {
            _importBasePath = importBasePath;
        }

        public FileBundle ProcessFile(PathAndContent file) {
            return ProcessFiles(new PathAndContent[] { file });
        }

        public FileBundle ProcessFiles(IEnumerable<PathAndContent> files) {
            FileBundle bundle = new FileBundle();

            foreach (PathAndContent file in files)
                ReadFile(bundle, file, null, true);     // Top level file - include in results

            return bundle;
        }


        // Algorithm...
        //
        // 1. Parse the file at 'path'
        // 2. Get the list of all external imported types
        // 3. Prepare list of types we are interested in within the import files...
        // 3.a  For initial file, we are interested in EVERYTHING 
        //   b  For imported files, we are only interested in new types which are embedded in Messages
        //      found in 'typesOfInterest'
        // 4. If the resulting list is non-empty, parse all imports
        //
        // NOTE: This could be improved in a couple of ways
        // a) Usage package name of tpye of interest to help choose imports
        // b) Rather than parsing the entire file, just get to the point where we've tokenized up to the package def
        //    and do not proceed further if that file defines a package in which we have no interest.

        // Example... (Note that unit tests exist just for this)
        //
        // # File a.proto
        // import "b.proto";
        // message msgA {
        //   b.msgB1            f1 = 1;
        //   b.msgB1.nestedB    f2 = 2;
        // }
        //
        // ==>   types-of-interest: [b.msgB1, b.msgB1.nestedB]
        //
        // # File b.proto
        // package b;
        // import "c.proto";
        // message msgB1 {                    // This is type 'b.msgB1'
        //   message nestedB {}               // This is type 'b.msgB1.nesstedB'
        // }
        // message msgB2 {
        //   c.msgC             f1 = 1;       // We will NOT parse the imports of B 
        //                                    // since c.msgC is NOT in types of interest
        // }
        internal void ReadFile(FileBundle bundle, PathAndContent pac, HashSet<string> typesOfInterest, bool includeInResults) {
            // TODO... This is too simplistic, because we may have different typesOfInterest
            // if (bundle.HasFile(path))
            //     return;

            // Step 1: Read and parse file
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(pac.Content));
            ProtobufParser parser = new ProtobufParser(tokenizer);
            File file = parser.Parse();
            file.Path = pac.Path;
            file.IncludeInResults = includeInResults;
            if (includeInResults)
                foreach (Message message in file.AllMessages())
                    message.IncludeInResults = true;
            bundle.AddFile(file);

            // Step 2
            HashSet<Type> importedTypes = FindImportedTypes(file);

            // Step 3
            HashSet<string> importedTypesOfInterest = new HashSet<string>();
            foreach (Type type in importedTypes) {
                Message owner = type.OwnerField.Owner as Message;
                if (typesOfInterest == null ||                                  // 3a above
                    typesOfInterest.Contains(owner.FullyQualifiedName())) {     // 3b above
                        owner.IncludeInResults = true;
                        importedTypesOfInterest.Add(type.Name);
                    }
            }

            // Step 4
            if (importedTypesOfInterest.Count > 0)
                foreach (Import import in file.Imports) {
                    string importPath = Path.Join(_importBasePath, import.ImportPath);
                    ReadFile(bundle, importPath, importedTypesOfInterest, false);   // Do not include in results indiscriminantly
                }
        }

        internal void ReadFile(FileBundle bundle, string path, HashSet<string> typesOfInterest, bool includeInResults) {
            PathAndContent pac = PathAndContent.Read(path);
            ReadFile(bundle, pac, typesOfInterest, includeInResults);
        }


        private HashSet<Type> FindImportedTypes(File file) {
            List<Type> types = new List<Type>();

            foreach (Message message in file.Messages)
                AddTypesForMessage(types, message);
            foreach (Extend extend in file.Extends)
                AddTypesForExtend(types, extend);

            return new HashSet<Type>(types.Where(x => x.IsImported));
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

        [JsonIgnore]
        public ReadOnlyDictionary<string, File> FileDict { get; private set; }
        public ReadOnlyDictionary<string, List<File>> PackageDict { get; private set; }

        internal FileBundle() {
            FileDict = new ReadOnlyDictionary<string, File>(_fileDict);
            PackageDict = new ReadOnlyDictionary<string, List<File>>(_packageDict);
        }

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
            if (!string.IsNullOrWhiteSpace(package)) {
                if (!_packageDict.TryGetValue(package, out List<File> files)) {
                    files = new List<File>();
                    _packageDict[package] = files;
                }
                files.Add(file);
            }
        }

        public IEnumerable<Message> AllMessages() {
            return _fileDict.Values
                .SelectMany(x => x.AllMessages())
                .Where(x => x.IncludeInResults);
        }

        public IEnumerable<EnumDef> AllEnumDefs() {
            return _fileDict.Values
                .SelectMany(x => x.AllEnumDefs());
        }

        public IEnumerable<Service> AllServices() {
            return _fileDict.Values
                .Where(x => x.IncludeInResults)
                .SelectMany(x => x.Services);
        }
    }
    #endregion
}
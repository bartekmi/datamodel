using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;

using Newtonsoft.Json;

using datamodel.schema.source.protobuf.data;

// This class takes care of imports in protobuf.

namespace datamodel.schema.source.protobuf {
    public class ProtobufImporter {
        private string _importBasePath;

        public ProtobufImporter(string importBasePath) {
            _importBasePath = importBasePath;
        }

        public FileBundle ProcessFile(PathAndContent pac) {
            return ProcessFiles(new PathAndContent[] { pac });
        }

        public FileBundle ProcessFiles(IEnumerable<PathAndContent> pacs) {
            FileBundle bundle = new FileBundle();
            foreach (PathAndContent pac in pacs)
                bundle.MaybeAddToBundle(pac);

            foreach (PathAndContent pac in pacs)
                ProcessFile(bundle, pac, null); 

            return bundle;
        }


        // Algorithm...
        //
        // >>> ProcessFile
        // 1. Parse the file at 'path' (only if never seen)
        // 2.a) if top-level file...
        //      - Mark file & all mesages as included
        //      - add all external types to "external types of interest"
        //   b) (else)... For all types of interest (strip package prefix)
        //     >>> Recurse Message of Interest
        //     3. Skip if already included
        //     4. Mark as included
        //     5. For all fields
        //        a) If field internal message, recurse to #3
        //        b) If field external, add to list of "external types of interest" for next level
        //
        // 6. If external types of interest non empty, process all imports
        //
        // NOTE: This could be improved in a couple of ways
        // a) Usage package name of tpye of interest to help choose imports
        // b) Rather than parsing the entire file, just get to the point where we've tokenized up to the package def
        //    and do not proceed further if that file defines a package in which we have no interest.
        internal void ProcessFile(FileBundle bundle, PathAndContent pac, HashSet<PbType> typesOfInterest) {
            // Step 1: Read and parse file
            PbFile file = bundle.MaybeAddToBundle(pac);

            HashSet<PbType> externalTypesOfInterest = new HashSet<PbType>();
            if (typesOfInterest == null) {
                file.IncludeInResults = true;
                foreach (Message message in file.AllMessages())
                    message.IncludeInResults = true;
                externalTypesOfInterest = new HashSet<PbType>(file.AllTypes());
            } else
                foreach (PbType typeOfInterest in typesOfInterest)
                    RecursivelyMarkInclude(file, externalTypesOfInterest, typeOfInterest);


            // Step 4
            if (externalTypesOfInterest.Count > 0)
                foreach (Import import in file.Imports) {
                    string importPath = Path.Join(_importBasePath, import.ImportPath);
                    PathAndContent importPac = PathAndContent.Read(importPath, false);
                    ProcessFile(bundle, importPac, externalTypesOfInterest); 
                }
        }

        private void RecursivelyMarkInclude(PbFile file, HashSet<PbType> externalTypesOfInterest, PbType type) {
            Message message = type.ResolveMessage(file);
            if (message != null) {
                if (message.IncludeInResults)
                    return;     // Already processed

                message.IncludeInResults = true;
                
                foreach (PbType childType in message.Fields.SelectMany(x => x.UsedTypes())) {
                    if (childType.IsImported)
                        externalTypesOfInterest.Add(childType);
                    else
                        RecursivelyMarkInclude(file, externalTypesOfInterest, childType);
                }
            }
        }
    }

    #region Helper Classes
    public class FileBundle {
        // Key: path, Value: parsed PbFile
        private Dictionary<string, PbFile> _fileDict = new Dictionary<string, PbFile>();
        private Dictionary<string, List<PbFile>> _packageDict = new Dictionary<string, List<PbFile>>();

        [JsonIgnore]
        public ReadOnlyDictionary<string, PbFile> FileDict { get; private set; }
        [JsonIgnore]
        public ReadOnlyDictionary<string, List<PbFile>> PackageDict { get; private set; }

        internal FileBundle() {
            FileDict = new ReadOnlyDictionary<string, PbFile>(_fileDict);
            PackageDict = new ReadOnlyDictionary<string, List<PbFile>>(_packageDict);
        }

        internal PbFile MaybeAddToBundle(PathAndContent pac) {
            if (!_fileDict.TryGetValue(pac.Path, out PbFile file)) {
                try {
                ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(pac.Content));
                ProtobufParser parser = new ProtobufParser(tokenizer);
                file = parser.Parse();
                file.Path = pac.Path;
                AddFile(file);
                } catch (Exception e) {
                    string message = "Error reading protobuf file: " + pac.Path;
                    throw new Exception(message, e);
                }
            }

            return file;
        }

        private void AddFile(PbFile file) {
            // Remember path => file
            string path = file.Path;
            if (_fileDict.ContainsKey(path))
                throw new Exception("Should never happen - trying to add path again: " + path);
            _fileDict[path] = file;

            // Remember package => list of files
            string package = file.Package;
            if (!string.IsNullOrWhiteSpace(package)) {
                if (!_packageDict.TryGetValue(package, out List<PbFile> files)) {
                    files = new List<PbFile>();
                    _packageDict[package] = files;
                }
                files.Add(file);
            }
        }

        public IEnumerable<Message> AllMessages {
            get {
                return _fileDict.Values
                    .SelectMany(x => x.AllMessages())
                    .Where(x => x.IncludeInResults);
            }
        }

        internal void RemoveComments() {
            foreach (var item in _fileDict.Values) { item.RemoveComments(); }
        }

        public IEnumerable<EnumDef> AllEnumDefs {
            get {
                return _fileDict.Values
                    .SelectMany(x => x.AllEnumDefs());
            }
        }

        public IEnumerable<Service> AllServices {
            get {
                return _fileDict.Values
                    .Where(x => x.IncludeInResults)
                    .SelectMany(x => x.Services);
            }
        }

        public bool ShouldSerializeAllMessages() { return AllMessages.Count() > 0; }
        public bool ShouldSerializeAllEnumDefs() { return AllEnumDefs.Count() > 0; }
        public bool ShouldSerializeAllServices() { return AllServices.Count() > 0; }
    }
    #endregion
}
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace datamodel.schema.source {

    #region Helper Classs
    public enum ParamType {
        String,
        Int,
        Float,
        Bool,
        File,
        FileOrDir,
        Url,
        Regex,
    }
    public class Parameter {
        public string Name;
        public string Description;
        public ParamType Type;
        public bool IsMandatory;
        public bool IsMultiple;
        public string Default;

        // The raw text value of the parameter
        public string Text { get; private set; }
        // The parsed value of the parameter, including, possibly file or url read
        public object Value { get; private set; }

        internal string Parse(string text) {
            Text = text;

            if (IsMultiple) {
                StringBuilder errorBuilder = new StringBuilder();
                string[] pieces = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
                List<object> values = new List<object>();
                Value = values;

                foreach (string piece in pieces) {
                    values.Add(ParseSingleWithCatch(piece.Trim(), out string error));
                    if (error != null)
                        errorBuilder.AppendLine(error);
                }

                return errorBuilder.Length == 0 ? null : errorBuilder.ToString();
            } else {
                Value = ParseSingleWithCatch(text, out string error);
                return error;
            }
        }

        private object ParseSingleWithCatch(string text, out string error) {
            error = null;

            try {
                return ParseSingle(text);
            } catch {
                error = string.Format("Could not parse/read {0} from '{1}'", Type, text);
                return null;
            }
        }

        internal virtual object ParseSingle(string text) {
            switch (Type) {
                case ParamType.String: return text;
                case ParamType.Int: return int.Parse(text);
                case ParamType.Float: return double.Parse(text);
                case ParamType.Bool: return bool.Parse(text);
                case ParamType.File: return File.ReadAllText(text);
                case ParamType.Url: return DownloadUrl(text);
                case ParamType.Regex: return new Regex(text);
                default:
                    throw new Exception("Unknown type; fix your code: " + Type);
            }
        }

        public static string DownloadUrl(string url) {
            using (WebClient client = new WebClient())
                return client.DownloadString(url);
        }

        internal void SetDefaultIfNeeded() {
            if (Value == null && Default != null) {
                string error = Parse(Default);
                if (error != null)
                    throw new Exception(string.Format("Default value for {0} could not be parsed; fix your code", Name));
            }
        }
    }
    public class ParameterFileOrDir : Parameter {
        public string FilePattern;
        public bool ReadContent = true;

        public ParameterFileOrDir() {
            Type = ParamType.FileOrDir;
        }

        internal override object ParseSingle(string path) {
            FileAttributes attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
                List<PathAndContent> files = new List<PathAndContent>();
                ReadRecursively(files, path);
                return new FileOrDir(true, files);
            } else
                return new FileOrDir(false, new List<PathAndContent>() { PathAndContent.Read(path, ReadContent)});
        }

        private void ReadRecursively(List<PathAndContent> files, string dir) {
            files.AddRange(Directory.GetFiles(dir, FilePattern).Select(x => PathAndContent.Read(x, ReadContent)));
            foreach (string nestedDir in Directory.GetDirectories(dir))
                ReadRecursively(files, nestedDir);
        }
    }
    public class FileOrDir {
        public bool IsDir { get; private set; }
        // Guaranteed to be only one if this IsDir is false
        public List<PathAndContent> Files { get; private set; }

        internal FileOrDir(bool isDir, List<PathAndContent> files) {
            IsDir = isDir;
            Files = files;
        }

        public static IEnumerable<PathAndContent> Combine(IEnumerable<FileOrDir> fileOrDirs) {
            return fileOrDirs.SelectMany(x => x.Files);
        }
    }
    public class PathAndContent {
        public string Path { get; private set; }

        private string _content;
        public string Content {
            get {
                if (_content == null)
                    _content = File.ReadAllText(Path);
                return _content;
            }
            set { _content = value; }
        }

        public PathAndContent(string path, string content) {
            Path = path;
            Content = content;
        }

        public static PathAndContent Read(string path, bool readContent = true) {
            string content = readContent ? File.ReadAllText(path) : null;
            return new PathAndContent(path, content);
        }
    }
    #endregion

    public class Parameters {
        private Dictionary<string, Parameter> _params;

        public const string GLOBAL_PARAM_TWEAKS = "tweaks";
        public const string GLOBAL_PARAM_NO_GRAPHVIZ = "nographviz";

        public Parameters(SchemaSource source, IEnumerable<string> commandLine) {
            _params = source.GetParameters().ToDictionary(x => x.Name);
            AddGlobalParameters();
            Parse(commandLine);
        }

        #region Global Parameters
        private void AddGlobalParameters() {
            Parameter[] globalParams = new Parameter[] {
                new Parameter() {
                    Name = GLOBAL_PARAM_TWEAKS,
                    Description = "Filename of JSON file which contains 'Tweaks' to the schema",
                    Type = ParamType.File,
                    IsMultiple = true
                },
                new Parameter() {
                    Name = GLOBAL_PARAM_NO_GRAPHVIZ,
                    Description = "Skip Graphviz generation. Useful for debugging, especially on systems where Graphiz is not installed.",
                    Type = ParamType.Bool,
                },
                // Add other global parameters here
            };

            foreach (Parameter param in globalParams)
                _params[param.Name] = param;
        }
        #endregion

        #region Parsing
        private void Parse(IEnumerable<string> commandLine) {
            StringBuilder builder = new StringBuilder();

            foreach (string paramAndValue in commandLine) {
                string[] pieces = paramAndValue.Split('=');

                if (pieces.Length != 2) {
                    builder.AppendLine("Expecting param=value, but got: " + paramAndValue);
                    continue;
                }

                string name = pieces[0];
                string value = pieces[1];

                if (!_params.TryGetValue(name, out Parameter parameter)) {
                    builder.AppendLine(string.Format("Parameter '{0}' is unexpected", name));
                    continue;
                }

                string error = parameter.Parse(value);
                if (error != null)
                    builder.AppendLine(error);
            }

            // Post-process...
            // 1. Check for missing mandatory parameters
            // 2. Apply default values
            foreach (Parameter parameter in _params.Values) {
                if (parameter.IsMandatory && parameter.Value == null)
                    builder.AppendLine(string.Format("Mandatory parameter '{0}' missing", parameter.Name));
                parameter.SetDefaultIfNeeded();
            }

            if (builder.Length > 0) {
                builder.AppendLine();
                builder.AppendLine("Valid Parameters for this Schema Source:");
                builder.AppendLine();
                AppendUsage(builder);
                throw new Exception(builder.ToString());
            }
        }
        #endregion

        #region Usage
        private void AppendUsage(StringBuilder builder) {
            foreach (Parameter parameter in _params.Values) {
                builder.AppendLine(string.Format("'{0}' ({1}{2}) - {3}:", 
                    parameter.Name, parameter.IsMultiple ? "[]" : "", parameter.Type,
                    parameter.IsMandatory ? "Mandatory" : "Optional"));
                builder.AppendLine("\t" + parameter.Description);
                if (parameter.Default != null)
                    builder.AppendLine("\tDefault Value: " + parameter.Default);
                builder.AppendLine();
            }
        }
        #endregion

        #region Public Accessor Methods
        public bool IsSet(string paramName) {
            if (!_params.TryGetValue(paramName, out Parameter parameter))
                throw new Exception("Inconsistent parameter requested; fix your code: " + paramName);

            return parameter.Value != null;
        }

        public string GetString(string paramName) {
            return GetParamValue(paramName, ParamType.String) as string;
        }

        public string[] GetStrings(string paramName) {
            object values = GetParamValue(paramName, ParamType.String, true);
            if (values == null)
                return new string[0];

            return ((List<object>)values).Cast<string>().ToArray();
        }

        public int? GetInt(string paramName) {
            return GetParamValue(paramName, ParamType.Int) as int?;
        }

        public double? GetDouble(string paramName) {
            return GetParamValue(paramName, ParamType.Float) as double?;
        }

        public bool GetBool(string paramName) {
            object value = GetParamValue(paramName, ParamType.Bool);
            return value is bool ? (bool)value : false;
        }

        public string GetFileContent(string paramName) {
            return GetParamValue(paramName, ParamType.File) as string;
        }

        public string[] GetFileContents(string paramName) {
            object values = GetParamValue(paramName, ParamType.File, true);
            if (values == null)
                return new string[0];

            return ((List<object>)values).Cast<string>().ToArray();
        }

        public string GetUrlContent(string paramName) {
            return GetParamValue(paramName, ParamType.Url) as string;
        }

        public Regex GetRegex(string paramName) {
            return GetParamValue(paramName, ParamType.Regex) as Regex;
        }

        public string GetRawText(string paramName) {
            return GetParameter(paramName).Text;
        }

        public FileOrDir GetFileOrDir(string paramName) {
            return GetParamValue(paramName, ParamType.FileOrDir) as FileOrDir;
        }

        public FileOrDir[] GetFileOrDirs(string paramName) {
            object values = GetParamValue(paramName, ParamType.FileOrDir, true);
            if (values == null)
                return new FileOrDir[0];

            return ((List<object>)values).Cast<FileOrDir>().ToArray();
        }

        private object GetParamValue(string paramName, ParamType type, bool isMultiple = false) {
            Parameter parameter = GetParameter(paramName);
            
            if (isMultiple != parameter.IsMultiple)
                throw new Exception("Inconsistent parameter multiplicity requested; fix your code: " + paramName);
            if (parameter.Type != type)
                throw new Exception("Inconsistent parameter type requested; fix your code: " + paramName);

            return parameter.Value;
        }

        private Parameter GetParameter(string paramName) {
            if (!_params.TryGetValue(paramName, out Parameter parameter))
                throw new Exception("Inconsistent parameter requested; fix your code: " + paramName);
            return parameter;
        }
        #endregion
    }
}
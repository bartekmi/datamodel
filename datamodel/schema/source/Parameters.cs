using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace datamodel.schema.source {

    public enum ParamType {
        String,
        Int,
        Float,
        Bool,
        File,
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

        public object Value { get; private set; }

        internal string Parse(string text) {
            if (IsMultiple) {
                StringBuilder builder = new StringBuilder();
                string[] pieces = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
                List<object> values = new List<object>();
                Value = values;

                foreach (string piece in pieces) {
                    values.Add(ParseSingle(piece.Trim(), out string error));
                    if (error != null)
                        builder.AppendLine(error);
                }

                return builder.Length == 0 ? null : builder.ToString();
            } else {
                Value = ParseSingle(text, out string error);
                return error;
            }
        }

        private object ParseSingle(string text, out string error) {
            error = null;

            try {
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
            } catch {
                error = string.Format("Could not parse/read {0} from '{1}'", Type, text);
                return null;
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

    public class Parameters {
        private Dictionary<string, Parameter> _params;

        public Parameters(SchemaSource source, IEnumerable<string> commandLine) {
            _params = source.GetParameters().ToDictionary(x => x.Name);
            Parse(commandLine);
        }

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

        public bool IsSet(string paramName) {
            if (!_params.TryGetValue(paramName, out Parameter parameter))
                throw new Exception("Inconsistent parameter requested; fix your code: " + paramName);

            return parameter.Value != null;
        }

        public string GetString(string paramName) {
            return GetParam(paramName, ParamType.String) as string;
        }

        public string[] GetStrings(string paramName) {
            object values = GetParam(paramName, ParamType.String, true);
            if (values == null)
                return new string[0];

            return ((List<object>)values).Cast<string>().ToArray();
        }

        public int? GetInt(string paramName) {
            return GetParam(paramName, ParamType.Int) as int?;
        }

        public double? GetDouble(string paramName) {
            return GetParam(paramName, ParamType.Float) as double?;
        }

        public bool GetBool(string paramName) {
            object value = GetParam(paramName, ParamType.Bool);
            return value is bool ? (bool)value : false;
        }

        public string GetFileContent(string paramName) {
            return GetParam(paramName, ParamType.File) as string;
        }

        public string[] GetFileContents(string paramName) {
            object values = GetParam(paramName, ParamType.File, true);
            if (values == null)
                return new string[0];

            return ((List<object>)values).Cast<string>().ToArray();
        }

        public string GetUrlContent(string paramName) {
            return GetParam(paramName, ParamType.Url) as string;
        }

        public Regex GetRegex(string paramName) {
            return GetParam(paramName, ParamType.Regex) as Regex;
        }

        private object GetParam(string paramName, ParamType type, bool isMultiple = false) {
            if (!_params.TryGetValue(paramName, out Parameter parameter))
                throw new Exception("Inconsistent parameter requested; fix your code: " + paramName);
            if (isMultiple != parameter.IsMultiple)
                throw new Exception("Inconsistent parameter multiplicity requested; fix your code: " + paramName);
            if (parameter.Type != type)
                throw new Exception("Inconsistent parameter type requested; fix your code: " + paramName);

            return parameter.Value;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace datamodel.schema.source.protobuf {
    public class ProtobufTokenizer {
        #region Helper Classes
        class Token {
            public string TheToken;
            public int LineNumber;
        }
        #endregion

        private List<Token> _tokens = new List<Token>();
        private int _index = 0;

        public ProtobufTokenizer(TextReader reader) {
            int lineNumber = 0;
            string raw = null;
            while ((raw = reader.ReadLine()) != null) {
                lineNumber++;
                string line = SanitizeLine(raw);
                if (line == null)
                    continue;

                string[] pieces = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
                _tokens.AddRange(pieces.Select(x => new Token() {
                    TheToken = x,
                    LineNumber = lineNumber,
                }));
            }
        }

        private string SanitizeLine(string raw) {
            string line = raw.Trim().Replace('\t', ' ');

            // Separate special chars with spaces for easier parsing
            foreach (string specialChar in new string[] { "[", "]", ";", "\"", "'"})
                line = line.Replace(specialChar, " " + specialChar + " ");

            return line;
        }

        #region Public Interface
        public string Next() {
            RememberIndex();
            return _tokens[_index++].TheToken;
        }

        public bool HasNext() {
            return _index < _tokens.Count;
        }

        public string Peek() {
            RememberIndex();
            return _tokens[_index].TheToken;
        }

        private int _oldIndex;
        private void RememberIndex() {
            _oldIndex = _index;
        }

        public int LineNumber { get { return _tokens[_oldIndex].LineNumber; } }

        #endregion
    }
}

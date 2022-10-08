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
            public override string ToString() {
                return string.Format("{0}:{1}", TheToken, LineNumber);
            }
        }
        #endregion

        private List<Token> _tokens = new List<Token>();
        private int _index = 0;
        private int _currentLine = 1;   // Only used during initial parse


        // Strategy...
        // Consume the characters one-by-one
        // If not in a string...
        //   - whitespace breaks token
        //   - The following characters are always tokens: ()<>[]=+-,;
        // The following characters delineate a string: " and '
        //   - String terminates on matching character
        //   - current delineating character can be escaped: \" and \'
        //   - whitespace is taken literally if in a string
        //
        // Parse states are:
        // - Normal
        // - In string
        // - In escape

        enum State {
            Normal,
            InString,
            InEscape,
        }

        private const string SINGLE_CHAR_TOKENS = "(){}<>[]=+-,;";

        private State ProcessNormal(StringBuilder builder, char c, out char quoteChar) {
            quoteChar = (char)0;

            if (SINGLE_CHAR_TOKENS.Contains(c)) {
                MaybeAddToken(builder);
                builder.Append(c);
                MaybeAddToken(builder);
                return State.Normal;
            }

            if (c == '"' || c == '\'') {
                quoteChar = c;
                return State.InString;
            }

            if (c == '\\')
                throw new Exception("Encountered backslash outside of string at line " + _currentLine);

            if (char.IsWhiteSpace(c))
                MaybeAddToken(builder);
            else
                builder.Append(c);      // Not checking for illegal characters

            return State.Normal;
        }

        private State ProcessInString(StringBuilder builder, char c, char quoteChar) {
            if (c == quoteChar) {
                MaybeAddToken(builder);
                return State.Normal;
            }

            if (c == '\\')
                return State.InEscape;

            builder.Append(c);
            return State.InString;
        }

        private State ProcessInEscape(StringBuilder builder, char c) {
            builder.Append(c);
            return State.InString;
        }

        private void MaybeAddToken(StringBuilder builder) {
            if (builder.Length == 0)
                return;     // No data to add

            _tokens.Add(new Token() {
                TheToken = builder.ToString(),
                LineNumber = _currentLine,
            });
            builder.Clear();
        }

        public ProtobufTokenizer(TextReader reader) {
            int c;
            State state = State.Normal;
            StringBuilder builder = new StringBuilder();
            char quoteChar = (char)0;

            while ((c = reader.Read()) != -1) {
                if (c == '\n')
                    _currentLine++;

                switch (state) {
                    case State.Normal:
                        state = ProcessNormal(builder, (char)c, out quoteChar);
                        break;
                    case State.InString:
                        state = ProcessInString(builder, (char)c, quoteChar);
                        break;
                    case State.InEscape:
                        state = ProcessInEscape(builder, (char)c);
                        break;
                    default:
                        throw new Exception("Unknown state: " + state);
                }
            }

            MaybeAddToken(builder);
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

        // Return line number of most recently read or peeked token
        public int LineNumber { get { return _tokens[_oldIndex].LineNumber; } }

        public string[] AllTokens() {
            return _tokens.Select(x => x.TheToken).ToArray();
        }

        public override string ToString() {
            StringBuilder builder = new StringBuilder();
            foreach (string token in _tokens.Select(x => x.TheToken))
                builder.AppendLine(token);
            return builder.ToString();
        }
        #endregion
    }
}

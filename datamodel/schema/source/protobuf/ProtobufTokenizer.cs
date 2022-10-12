using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace datamodel.schema.source.protobuf {
    public class ProtobufTokenizer {
        #region Helper Classes
        internal class Token {
            public string TheToken;
            public string Comment;
            public int LineNumber;
            public int CharNumber;
            
            public override string ToString() {
                return string.Format("{0}:{1}", TheToken, LineNumber);
            }
        }
        #endregion

        private List<Token> _tokens = new List<Token>();
        private int _index = 0;

        // Only used during initial parse
        private int _currentLine = 1;  
        private int _currentChar = 1; 
        private StringBuilder _tokenBuilder = new StringBuilder();
        private StringBuilder _commentBuilder = new StringBuilder();
        private List<Token> _currentLineTokens = new List<Token>();


        // Strategy...
        // Consume the characters one-by-one
        // If not in a string...
        //   - whitespace breaks token
        //   - Characters in SINGLE_CHAR_TOKENS are always tokens
        // The following characters delineate a string: " and '
        //   - String terminates on matching character
        //   - current delineating character can be escaped: \" and \'
        //   - whitespace is taken literally if in a string
        //
        //
        // Comment handling...
        // - All comments that preceed a line and comments at the end of a line
        //   are attributed to that line... Example:
        //
        // // All comments here (with newline)
        // /* ...and here (with newline)... */
        // hare are some "tokens" // Are attributed to tokens on this line
        //
        // This suggests the following algorithm:
        //
        // - if we encounter a new-line (not in quoted string or in /**/)...
        //   - ...assign current comments to all tokens on this line iff count > 0
        //   - clear the comment buffer
        //   - clear the current-line tokens
        // - special case for EOF since newline is not guaranteed

        enum State {
            Normal,
            InString,
            InEscape,
            InSlash,
            InSlashSlash,
            InSlashStar,
            InSlashStarStar,
        }

        private const string SINGLE_CHAR_TOKENS = "(){}<>[]=+-,;";

        #region Main Parse Code
        public ProtobufTokenizer(TextReader reader) {
            int c;
            State state = State.Normal;
            char quoteChar = (char)0;

            while ((c = reader.Read()) != -1) {
                if (c == '\n') {
                    _currentLine++;
                    _currentChar = 1;
                } else 
                    _currentChar++;

                switch (state) {
                    case State.Normal:
                        state = ProcessNormal((char)c, out quoteChar);
                        break;
                    case State.InString:
                        state = ProcessInString((char)c, quoteChar);
                        break;
                    case State.InEscape:
                        state = ProcessInEscape((char)c);
                        break;
                    case State.InSlash:
                        state = ProcessInSlash((char)c);
                        break;
                    case State.InSlashSlash:
                        state = ProcessInSlashSlash((char)c);
                        break;
                    case State.InSlashStar:
                        state = ProcessInSlashStar((char)c);
                        break;
                    case State.InSlashStarStar:
                        state = ProcessInSlashStarStar((char)c);
                        break;
                    default:
                        throw new Exception("Unknown state: " + state);
                }

                if (c == '\n') 
                    if (state == State.Normal || state == State.InSlashSlash)
                        MaybeAttributeCommentsToCurrentLine();
            }

            MaybeAddToken();
            MaybeAttributeCommentsToCurrentLine();
        }
        #endregion

        #region State-specific Process Methods
        private State ProcessNormal(char c, out char quoteChar) {
            quoteChar = (char)0;

            if (SINGLE_CHAR_TOKENS.Contains(c)) {
                MaybeAddToken();
                _tokenBuilder.Append(c);
                MaybeAddToken();
                return State.Normal;
            }

            if (c == '"' || c == '\'') {
                quoteChar = c;
                return State.InString;
            }

            if (c == '/')
                return State.InSlash;

            if (c == '\\')
                throw new Exception("Encountered backslash outside of string literal at line " + _currentLine);

            if (char.IsWhiteSpace(c))
                MaybeAddToken();
            else
                _tokenBuilder.Append(c);      // Not checking for illegal characters

            return State.Normal;
        }

        private State ProcessInString(char c, char quoteChar) {
            if (c == quoteChar) {
                MaybeAddToken();
                return State.Normal;
            }

            if (c == '\\')
                return State.InEscape;

            _tokenBuilder.Append(c);
            return State.InString;
        }

        private State ProcessInEscape(char c) {
            _tokenBuilder.Append(c);
            return State.InString;
        }

        private State ProcessInSlash(char c) {
            if (c == '/')
                return State.InSlashSlash;
            if (c == '*')
                return State.InSlashStar;
            throw new Exception("Expected // or /*");
        }

        private State ProcessInSlashSlash(char c) {
            _commentBuilder.Append(c);
            return c == '\n' ? State.Normal : State.InSlashSlash;
        }

        private State ProcessInSlashStar(char c) {
            if (c == '*')
                return State.InSlashStarStar;

            _commentBuilder.Append(c);
            return State.InSlashStar;
        }

        private State ProcessInSlashStarStar(char c) {
            if (c == '/') {
                _commentBuilder.Append('\n');
                return State.Normal;
            }

            _commentBuilder.Append('*');    // Write the '*' character into comments - it was not part of */
            return State.InSlashStar;
        }
        #endregion

        #region Utility Methods
        private void MaybeAddToken() {
            if (_tokenBuilder.Length == 0)
                return;     // No data to add

            Token token = new Token() {
                TheToken = _tokenBuilder.ToString(),
                LineNumber = _currentLine,
                CharNumber = _currentChar,
            };

            _tokens.Add(token);
            _currentLineTokens.Add(token);

            _tokenBuilder.Clear();
        }

        private void MaybeAttributeCommentsToCurrentLine() {
            if (_currentLineTokens.Count == 0)
                return; // Nothing to attribute to... Keep collecting.

            string comment = _commentBuilder.ToString().TrimEnd();

            foreach (Token token in _currentLineTokens)
                token.Comment = comment;
            
            _commentBuilder.Clear();
            _currentLineTokens.Clear();
        }
        #endregion

        #region Public Interface
        public string Next() {
            return NextToken().TheToken;
        }

        internal Token NextToken() {
            RememberIndex();
            return _tokens[_index++];
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
        public string Comment { get { return _tokens[_oldIndex].Comment; }}

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

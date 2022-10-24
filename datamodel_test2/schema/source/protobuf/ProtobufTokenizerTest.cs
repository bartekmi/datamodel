using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

namespace datamodel.schema.source.protobuf {
    public class ProtobufTokenizerTest {

        private readonly ITestOutputHelper _output;

        public ProtobufTokenizerTest(ITestOutputHelper output) {
            _output = output;
        }

        #region Single Tokens
        [Fact]
        public void TokenizeInt() {
            TokenizeSingle("1234");
        }

        [Fact]
        public void TokenizeSymbol() {
            TokenizeSingle("=");
        }

        [Fact (Skip = "We don't need this until we care about options")]
        public void TokenizeFloat() {
            TokenizeSingle("1234.567e10");
        }

        [Fact]
        public void TokenizeQuotedString() {
            TokenizeSingle("\"Don't count your chickens\"", "Don't count your chickens");
            TokenizeSingle("'He said to me, \"Smile!\".'", "He said to me, \"Smile!\".");
        }
        #endregion

        #region Multiple Tokens
        [Fact]
        public void TokenizeSyntax() {
            Tokenize("syntax = 'proto3';", "syntax", "=", "proto3", ";");
        }

        [Fact]
        public void TokenizeDoubleBrace() {
            Tokenize("{}", "{", "}");
        }
        #endregion

        [Fact]
        public void TokenizeCommentsMinimal() {
            TokenizeComments(
@"// c
a b
", 2, 
@" c");
        }


        [Fact]
        public void TokenizeCommentsSlashSlash() {
            TokenizeComments(
@"
// Line 1
// Line 2
one two three
", 3, 
@" Line 1
 Line 2");
        }

        [Fact]
        public void TokenizeCommentsSlashSlashSameLine() {
            TokenizeComments(
@"
one two three           // End line
", 3, 
@" End line");
        }

        [Fact]
        public void TokenizeCommentsSlashSlashSameLineNoEOL() {
            TokenizeComments(
@"
one two three           // End line", 3, 
@" End line");
        }

        [Fact]
        public void TokenizeCommentsSlashStar() {
            TokenizeComments(
@"
/*
Line 1
Line 2
Line 3
*/
one two three
", 3, 
@"
Line 1
Line 2
Line 3");
        }

        private void TokenizeComments(string proto, int tokenCount, string expectedComments) {
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(proto));
            Assert.Equal(tokenCount, tokenizer.AllTokens().Length);

            while (tokenizer.HasNext())
                Assert.Equal(expectedComments, tokenizer.NextToken().Comment);
        }

        private void TokenizeSingle(string rawToken, string expected = null) {
            string text = string.Format("first {0} last", rawToken);
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(text));
            if (expected == null)
                expected = rawToken;

            Assert.Equal("first", tokenizer.Next());
            Assert.Equal(expected, tokenizer.Next());
            Assert.Equal("last", tokenizer.Next());
        }

        private void Tokenize(string proto, params string[] expected) {
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(proto));
            List<string> tokens = new List<string>();

            while (tokenizer.HasNext())
                tokens.Add(tokenizer.Next());

            Assert.Equal(
                string.Join("|", expected), 
                string.Join("|", tokens.ToArray()));
        }
    }
}
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

        [Fact]
        public void TokenizeIdentifier() {
            TokenizeSingle("abc.def1");
        }

        [Fact]
        public void TokenizeInt() {
            TokenizeSingle("1234");
        }

        [Fact]
        public void TokenizeSymbol() {
            TokenizeSingle("=");
        }

        [Fact]
        public void TokenizeFloat() {
            TokenizeSingle("1234.567e10");
        }

        [Fact]
        public void TokenizeQuotedString() {
            TokenizeSingle("\"Don't count your chickens\"", "Don't count your chickens");
            TokenizeSingle("'He said to me, \"Smile!\".'", "He said to me, \"Smile!\".");
        }

        [Fact]
        public void TokenizeSyntax() {
            Tokenize("syntax = 'proto3';", "syntax", "=", "proto3", ";");
        }

        [Fact]
        public void TokenizePackage() {
            Tokenize("package my.pkg;", "package", "my.pkg", ";");
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

        private void Tokenize(string text, params string[] expected) {
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(text));
            List<string> tokens = new List<string>();

            while (tokenizer.HasNext())
                tokens.Add(tokenizer.Next());

            Assert.Equal(
                string.Join("|", expected), 
                string.Join("|", tokens.ToArray()));
        }
    }
}
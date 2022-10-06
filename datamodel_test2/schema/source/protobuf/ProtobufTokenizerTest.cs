using System;
using System.Text;
using System.IO;

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
            Tokenize("abc.def1");
        }

        [Fact]
        public void TokenizeInt() {
            Tokenize("1234");
        }

        [Fact]
        public void TokenizeFloat() {
            Tokenize("1234.567e10");
        }

        [Fact]
        public void TokenizeQuotedString() {
            Tokenize("\"Don't count your chickens\"", "Don't count your chickens");
            Tokenize("'He said to me, \"Smile!\".'", "He said to me, \"Smile!\"");
        }

        private void Tokenize(string rawToken, string expected = null) {
            StringBuilder builder = new StringBuilder();
            builder.Append("first");
            builder.Append(" ");
            builder.Append(rawToken);
            builder.Append(" ");
            builder.Append("last");

            string text = builder.ToString();
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(text));
            if (expected == null)
                expected = rawToken;

            Assert.Equal("first", tokenizer.Next());
            Assert.Equal(expected, tokenizer.Next());
            Assert.Equal("last", tokenizer.Next());
        }
    }


}
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

namespace datamodel.schema.source.protobuf {
    public class ProtobufCommentImporterTest {

        private readonly ITestOutputHelper _output;

        public ProtobufCommentImporterTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void SingleLine() {
            RunTest(" \t1234\t ", "1234");
        }        
        
        [Fact]
        public void OneParagraph() {
            RunTest(@"
 The quick brown   
 fox jumped over        
 the lazy dog.
 ", 
            "The quick brown fox jumped over the lazy dog.");
        }
        
        [Fact]
        public void MultipleParagraphs() {
            RunTest(@"
 The quick brown   
 fox jumped over        
 the lazy dog.

 How can this possibly
 end when all I
 see is darkness?


 Go eat icecream.
 ", 

@"The quick brown fox jumped over the lazy dog.
How can this possibly end when all I see is darkness?

Go eat icecream.");
        }

        private void RunTest(string raw, string expected) {
            string processed = ProtobufCommentProcessor.ProcessComments(raw);
            Assert.Equal(expected, processed);
        }
    }
}
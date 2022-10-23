using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

using datamodel.utils;

namespace datamodel.schema.source.protobuf.data {
    public class PbTypeTest {

        private readonly ITestOutputHelper _output;

        public PbTypeTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void ResolveInternalMessage() {
            PbFile file = ReadProto(@"
package p;

message a {
  a f1 = 1;         // p.a
  aa f2 = 2;        // p.a.aa
  aa.aaa f3 = 3;    // p.a.aa.aaa

  a.aa f4 = 4;      // p.a.aa
  a.aa.aaa f5 = 5;  // p.a.aa.aaa
  p.a.aa.aaa f6 = 6;  // p.a.aa.aaa

  message aa {
    a f101 = 1;       // p.a
    aa f102 = 2;      // p.a.aa
    aaa f103 = 3;     // p.a.aa.aaa

    a.aa f104 = 4;          // p.a.aa
    p.a.aa f105 = 5;        // p.a.aa

    aa.aaa f110 = 110         // p.a.aa.aaa
    a.aa.aaa f111 = 111;      // p.a.aa.aaa
    p.a.aa.aaa f117 = 117;    // p.a.aa.aaa


    message aaa {
      a f201 = 1;     // p.a
      aa f202 = 2;   // p.a.aa
      aaa f203 = 3;  // p.a.aa.aaa
    }
  }
}");

            _output.WriteLine(JsonFormattingUtils.JsonPretty(file));
            Assert.Equal(3, file.AllMessages().Count());

            int cases = 0;
            foreach (FieldNormal field in file.AllMessages().SelectMany(x => x.Fields).Cast<FieldNormal>()) {
                _output.WriteLine(string.Format("Processing field {0} of type {1} with comment {2}", 
                  field.Name, field.Type, field.Comment));

                if (!string.IsNullOrEmpty(field.Comment)) {
                  Message resolved = field.Type.ResolveInternalMessage();
                  Assert.NotNull(resolved);
                  Assert.Equal(field.Comment.Trim(), resolved.QualifiedName());
                  cases++;
                }
            }

            _output.WriteLine("Number of test cases: " + cases);
            Assert.Equal(17, cases);
        }

        private PbFile ReadProto(string proto) {
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(proto));
            ProtobufParser parser = new ProtobufParser(tokenizer);
            return parser.Parse();
        }        
   }
}
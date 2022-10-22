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

  message aa {
    a f6 = 1;       // p.a
    aa f7 = 2;      // p.a.aa
    aaa f8 = 3;     // p.a.aa.aaa
    message aaa {
      a f9 = 1;     // p.a
      aa f10 = 2;   // p.a.aa
      aaa f11 = 3;  // p.a.aa.aaa
    }
  }
}");

            _output.WriteLine(JsonFormattingUtils.JsonPretty(file));
            Assert.Equal(3, file.AllMessages().Count());

            int cases = 0;
            foreach (FieldNormal field in file.AllMessages().SelectMany(x => x.Fields).Cast<FieldNormal>()) {
                _output.WriteLine(string.Format("Processing field {0} with comment {1}", field.Name, field.Comment));

                if (!string.IsNullOrEmpty(field.Comment)) {
                  Message resolved = field.Type.ResolveInternalMessage();
                  Assert.NotNull(resolved);
                  Assert.Equal(field.Comment.Trim(), resolved.QualifiedName());
                  cases++;
                }
            }

            Assert.Equal(11, cases);
        }

        private PbFile ReadProto(string proto) {
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(proto));
            ProtobufParser parser = new ProtobufParser(tokenizer);
            return parser.Parse();
        }        
   }
}
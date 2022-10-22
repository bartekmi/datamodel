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
  a af = 1;         // p.a
  aa aaf = 2;       // p.a.aa
  aa.aaa aaaf = 3;  // p.a.aa.aaa

  a.aa f4 = 4;      // p.a.aa
  a.aa.aaa f5 = 5;  // p.aa.aaa

  message aa {
    a af = 1;
    aa aaf = 2;
    aaa aaaf = 3;
    message aaa {
      a af = 1;
      aa aaf = 2;
      aaa aaaf = 3;
    }
  }
}");

            _output.WriteLine(JsonFormattingUtils.JsonPretty(file));
            Assert.Equal(3, file.AllMessages().Count());

            int cases = 0;
            foreach (FieldNormal field in file.AllMessages().SelectMany(x => x.Fields).Cast<FieldNormal>()) {
                _output.WriteLine(string.Format("Processing field {0} with comment {1}", field.Name, field.Comment));

                if (!string.IsNullOrEmpty(field.Comment)) {
                    Assert.Equal(field.Comment.Trim(), field.Type.ResolveInternalMessage().QualifiedName());
                    cases++;
                }
            }

            Assert.Equal(5, cases);
        }

        private PbFile ReadProto(string proto) {
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(proto));
            ProtobufParser parser = new ProtobufParser(tokenizer);
            return parser.Parse();
        }        
   }
}
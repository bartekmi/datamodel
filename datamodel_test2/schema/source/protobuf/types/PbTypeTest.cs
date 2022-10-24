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
            string proto = @"
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
}";

            RunTest(proto, 17, TestType.Message);
        }

        [Fact]
        public void ResolveInternalEnumDef() {
            string proto = @"
package p;

enum e { one = 0; }

message a {
  e f1 = 1;                     // p.e
  ee f2 = 2;                    // p.a.ee
  aa.eee f3 = 3;                // p.a.aa.eee
  a.ee f4 = 4;                  // p.a.ee
  a.aa.eee f5 = 5;              // p.a.aa.eee
  p.a.aa.eee f6 = 6;            // p.a.aa.eee

  enum ee { one = 0; }

  message aa {
    e f1 = 1;                   // p.e
    ee f2 = 2;                  // p.a.ee
    eee f3 = 3;                 // p.a.aa.eee
    aa.aaa.eeee f35 = 35;       // p.a.aa.aaa.eeee
    a.aa.aaa.eeee f4 = 4;       // p.a.aa.aaa.eeee
    p.a.aa.aaa.eeee f5 = 5;     // p.a.aa.aaa.eeee
    aaa.eeee f6= 6;             // p.a.aa.aaa.eeee

    enum eee { one = 0; }

    message aaa {
      e f1 = 1;                 // p.e
      ee f2 = 2;                // p.a.ee
      eee f3 = 3;               // p.a.aa.eee
      eeee f4 = 4;              // p.a.aa.aaa.eeee

      enum eeee { one = 0; }
    }
  }
}";

            RunTest(proto, 17, TestType.EnumDef);
        }


        [Fact]
        public void ResolveInternalMessageLeadingDot() {
            string proto = @"
package p;

message a {
  message b {}
  b f1 = 1;         // p.a.b
  .b f2 = 2;        // p.b
}
message b {}
";

            RunTest(proto, 2, TestType.Message);
        }


        #region Utilities
        enum TestType {
            Message,
            EnumDef,
        }

        private void RunTest(string proto, int expectedTestCases, TestType testType) {
            PbFile file = ReadProto(proto);
            _output.WriteLine(JsonFormattingUtils.JsonPretty(file));
            Assert.Equal(3, file.AllMessages().Count());

            int cases = 0;
            foreach (FieldNormal field in file.AllMessages().SelectMany(x => x.Fields).Cast<FieldNormal>()) {
                _output.WriteLine(string.Format("Processing field {0} of type {1} with comment {2}", 
                  field.Name, field.Type, field.Comment));

                if (!string.IsNullOrEmpty(field.Comment)) {
                  field.Type.ResolveInternal(out Message message, out EnumDef enumDef);
                  Owned owned = testType == TestType.Message ? message : enumDef;
                  Assert.NotNull(owned);
                  Assert.Equal(field.Comment.Trim(), owned.QualifiedName());
                  cases++;
                }
            }

            _output.WriteLine("Number of test cases: " + cases);
            Assert.Equal(expectedTestCases, cases);
        }

        private PbFile ReadProto(string proto) {
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(proto));
            ProtobufParser parser = new ProtobufParser(tokenizer);
            return parser.Parse();
        }    
        #endregion    
   }
}
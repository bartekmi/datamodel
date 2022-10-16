using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

using datamodel.schema.tweaks;
using datamodel.utils;

namespace datamodel.schema.source.protobuf {
    public class ProtobufSourceTest {

        private readonly ITestOutputHelper _output;

        public ProtobufSourceTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void ParseSingleMessage() {
            string proto = @"
// Message description
message myMessage {
    string field1 = 1;              // Field description
    repeated int32 field2 = 2;
}
";

            RunTest(proto, @"
 {
   Title: myproto.proto,
   Models: {
     myMessage: {
       Name: myMessage,
       QualifiedName: myMessage,
       Description:  Message description,
       AllProperties: [
         {
           Name: field1,
           Description:  Field description,
           DataType: string
         },
         {
           Name: field2,
           DataType: []int32
         }
       ]
     }
   }
 }");
        }

        [Fact]
        public void ParseSingleMessageOneOf() {
            string proto = @"
// Message description
message myMessage {
  // Oneof description
  oneof myOneof {
    string field1 = 1;              // Field description
    int32 field2 = 2;
  }
}
";

            RunTest(proto, @"
 {
   Title: myproto.proto,
   Models: {
     myMessage: {
       Name: myMessage,
       QualifiedName: myMessage,
       Description:  Message description,
       AllProperties: [
         {
           Name: field1,
           Description: One-of Group: myOneof\n\n Oneof description\n\n Field description,
           DataType: string
         },
         {
           Name: field2,
           Description: One-of Group: myOneof\n\n Oneof description,
           DataType: int32
         }
       ]
     }
   }
 }");
        }

        [Fact]
        public void ParseSingleMessageMap() {
            string proto = @"
// Message description
message myMessage {
  map<int32, string> stringMap = 1;
  map<int32, myMessage> msgMap = 2;
}
";

            RunTest(proto, @"
 {
   Title: myproto.proto,
   Models: {
     myMessage: {
       Name: myMessage,
       QualifiedName: myMessage,
       Description:  Message description,
       AllProperties: [
         {
           Name: stringMap,
           DataType: [int32]string
         }
       ]
     }
   },
   Associations: [
     {
       OwnerSide: myMessage,
       OwnerMultiplicity: Aggregation,
       OtherSide: myMessage,
       OtherRole: msgMap,
       OtherMultiplicity: Many
     }
   ]
 }");
        }

        [Fact]
        public void ParseEnumField() {
            string proto = @"
message myMessage {
    // Enum description
    enum myEnum {
        one = 1;    // First
        two = 2;    // Second
    }
    myEnum field1 = 1;
}
";

            RunTest(proto, @"
 {
   Title: myproto.proto,
   Models: {
     myMessage: {
       Name: myMessage,
       QualifiedName: myMessage,
       AllProperties: [
         {
           Name: field1,
           DataType: myEnum,
           Enum: {
             Name: myEnum,
             Description:  Enum description,
             Values: [
               {
                 Key: one,
                 Value:  First
               },
               {
                 Key: two,
                 Value:  Second
               }
             ]
           }
         }
       ]
     }
   }
 }");
        }

        [Fact]
        public void ParseAssociation() {
            string proto = @"
message myMessage {
    message myNested {
        string f1 = 1;
    }
    myNested f1 = 1;                // Association description
    repeated myNested f2 = 2;
}
";

            RunTest(proto, @"
 {
   Title: myproto.proto,
   Models: {
     myMessage: {
       Name: myMessage,
       QualifiedName: myMessage
     },
     myNested: {
       Name: myNested,
       QualifiedName: myNested,
       AllProperties: [
         {
           Name: f1,
           DataType: string
         }
       ]
     }
   },
   Associations: [
     {
       OwnerSide: myMessage,
       OwnerMultiplicity: Aggregation,
       OtherSide: myNested,
       OtherRole: f1,
       OtherMultiplicity: One,
       Description:  Association description
     },
     {
       OwnerSide: myMessage,
       OwnerMultiplicity: Aggregation,
       OtherSide: myNested,
       OtherRole: f2,
       OtherMultiplicity: Many
     }
   ]
 }");
        }

        #region Utilities
        private void RunTest(string protoContent, string expected) {
            ProtobufSource source = new ProtobufSource();
            source.InitializeInternal("/my/dir/myproto.proto", protoContent);

            TempSource tempSource = TempSource.CloneFromSource(source);
            string actual = tempSource.ToJasonNoQuotes();
            expected = JsonFormattingUtils.DeleteFirstSpace(expected);

            if (actual != expected) {
                _output.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                _output.WriteLine(actual);      // We do this to get actual in full glory
                _output.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");

                Assert.Equal(expected, actual);
            }
        }
        #endregion
    }
}
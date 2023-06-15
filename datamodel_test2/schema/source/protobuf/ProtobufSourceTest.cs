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
   Models: {
     myMessage: {
       Name: myMessage,
       QualifiedName: myMessage,
       Description:  Message description,
       AllProperties: [
         {
           DataType: string,
           Name: field1,
           Description:  Field description
         },
         {
           DataType: []int32,
           Name: field2
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
   Models: {
     myMessage: {
       Name: myMessage,
       QualifiedName: myMessage,
       Description:  Message description,
       AllProperties: [
         {
           DataType: string,
           Name: field1,
           Description: One-of Group: myOneof\n\n Oneof description\n\n Field description
         },
         {
           DataType: int32,
           Name: field2,
           Description: One-of Group: myOneof\n\n Oneof description
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
   Models: {
     myMessage: {
       Name: myMessage,
       QualifiedName: myMessage,
       Description:  Message description,
       AllProperties: [
         {
           DataType: [int32]string,
           Name: stringMap
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
package p;
// Enum description
enum e { 
  z = 0;        // Item description
}
message m1 {
  enum ee { z1 = 0; }

  e f1            = 1;
  ee f2           = 2;
  p.m1.n1.eee f3  = 3;
  m2.ee f4        = 4;

  message n1 {
    enum eee {}
  }
}
message m2 {
  enum ee { z2 = 0; }
}
";

            RunTest(proto, @"
 {
   Models: {
     p.m1: {
       Name: m1,
       QualifiedName: p.m1,
       Levels: [
         p
       ],
       AllProperties: [
         {
           DataType: e,
           Enum: {
             Name: e,
             Description:  Enum description,
             Values: {
               z:  Item description
             }
           },
           Name: f1
         },
         {
           DataType: ee,
           Enum: {
             Name: ee,
             Values: {
               z1: 
             }
           },
           Name: f2
         },
         {
           DataType: p.m1.n1.eee,
           Enum: {
             Name: eee,
             Values: {}
           },
           Name: f3
         },
         {
           DataType: m2.ee,
           Enum: {
             Name: ee,
             Values: {
               z2: 
             }
           },
           Name: f4
         }
       ]
     },
     p.m1.n1: {
       Name: n1,
       QualifiedName: p.m1.n1,
       Levels: [
         p
       ]
     },
     p.m2: {
       Name: m2,
       QualifiedName: p.m2,
       Levels: [
         p
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
   Models: {
     myMessage: {
       Name: myMessage,
       QualifiedName: myMessage
     },
     myMessage.myNested: {
       Name: myNested,
       QualifiedName: myMessage.myNested,
       AllProperties: [
         {
           DataType: string,
           Name: f1
         }
       ]
     }
   },
   Associations: [
     {
       OwnerSide: myMessage,
       OwnerMultiplicity: Aggregation,
       OtherSide: myMessage.myNested,
       OtherRole: f1,
       OtherMultiplicity: One,
       Description:  Association description
     },
     {
       OwnerSide: myMessage,
       OwnerMultiplicity: Aggregation,
       OtherSide: myMessage.myNested,
       OtherRole: f2,
       OtherMultiplicity: Many
     }
   ]
 }");
        }

        [Fact]
        public void ParseService() {
            string proto = @"
service SearchService {
  rpc Search(SearchRequest) returns (SearchResponse);
}";

            RunTest(proto, @"
 {
   Models: {
     SearchService: {
       Name: SearchService,
       QualifiedName: SearchService,
       Methods: [
         {
           Inputs: [
             {
               Type: {
                 Name: SearchRequest
               }
             }
           ],
           Outputs: [
             {
               Type: {
                 Name: SearchResponse
               }
             }
           ],
           Name: Search
         }
       ]
     }
   },
   Associations: [
     {
       OwnerSide: SearchService,
       OwnerMultiplicity: Aggregation,
       OtherSide: SearchRequest,
       OtherMultiplicity: One
     },
     {
       OwnerSide: SearchService,
       OwnerMultiplicity: Aggregation,
       OtherSide: SearchResponse,
       OtherMultiplicity: One
     }
   ]
 }");
        }

        [Fact]
        public void AssociationToImportedProto() {
            string proto1 = @"
package a;
import 'b.proto';

message msgA {
  b.msgB f1 = 1;
  msgA2 f2 = 2;
  message msgA2 {}
}
";

            string proto2 = @"
package b;
message msgB {}
";

            string expected = @"
 {
   Models: {
     a.msgA: {
       Name: msgA,
       QualifiedName: a.msgA,
       Levels: [
         a
       ]
     },
     a.msgA.msgA2: {
       Name: msgA2,
       QualifiedName: a.msgA.msgA2,
       Levels: [
         a
       ]
     },
     b.msgB: {
       Name: msgB,
       QualifiedName: b.msgB,
       Levels: [
         b
       ]
     }
   },
   Associations: [
     {
       OwnerSide: a.msgA,
       OwnerMultiplicity: Aggregation,
       OtherSide: b.msgB,
       OtherRole: f1,
       OtherMultiplicity: One
     },
     {
       OwnerSide: a.msgA,
       OwnerMultiplicity: Aggregation,
       OtherSide: a.msgA.msgA2,
       OtherRole: f2,
       OtherMultiplicity: One
     }
   ]
 }";

            RunMultiFileTest(expected, 
              new PathAndContent("a.proto", proto1),
              new PathAndContent("b.proto", proto2));
        }

        #region Utilities
        private void RunTest(string protoContent, string expected) {
          RunMultiFileTest(expected, new PathAndContent("", protoContent));
        }

        private void RunMultiFileTest(string expected, params PathAndContent[] pacs) {
            ProtobufImporter importer = new ProtobufImporter(null);
            FileBundle bundle = importer.ProcessFiles(pacs);
            ProtobufSource source = new ProtobufSource();
            source.InitializeInternal(bundle);

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
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

using datamodel.schema.tweaks;

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
       AllColumns: [
         {
           Name: field1,
           DataType: string,
           Description:  Field description
         },
         {
           Name: field2,
           DataType: []int32
         }
       ]
     }
   },
   Associations: []
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
       AllColumns: [
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
   },
   Associations: []
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
       QualifiedName: myMessage,
       AllColumns: []
     },
     myNested: {
       Name: myNested,
       QualifiedName: myNested,
       AllColumns: [
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
            expected = DeleteFirstSpace(expected);

            if (actual != expected) {
                _output.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                _output.WriteLine(actual);      // We do this to get actual in full glory
                _output.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");

                Assert.Equal(expected, actual);
            }
        }

        // Annoyingly, the text captured in _output adds a space in front of
        // every line. This prevents us from simply copy-and-pasting it into
        // the "expected" value - we must remove this space from every line
        // to avoid the toil of removing these spaces manually.
        private string DeleteFirstSpace(string text) {
            StringBuilder builder = new StringBuilder();
            using (StringReader reader = new StringReader(text)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    if (line == "")
                        continue;   // Ignore blank lines (at start or end)
                    if (line.StartsWith(" "))
                        builder.AppendLine(line.Substring(1));
                    else 
                        return text;    // Not all lines start with space - just return original
                }
            }

            return builder.ToString().Trim();
        }
        #endregion
    }
}
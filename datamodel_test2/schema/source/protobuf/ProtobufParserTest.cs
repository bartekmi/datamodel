using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

using datamodel.utils;
using datamodel.schema.source.protobuf.data;
namespace datamodel.schema.source.protobuf {
    public class ProtobufPerserTest {
        private const bool DUMP_TOKENS = false;

        private readonly ITestOutputHelper _output;

        public ProtobufPerserTest(ITestOutputHelper output) {
            _output = output;
        }

        #region Protobuf 2 & 3 - Shared
        [Fact]
        public void ParseFileLevelProperties() {
            string proto = @"
//File Comment
syntax = 'proto3';

import 'my/dir/one.proto';
import weak 'my/dir/two.proto';
import public 'my/dir/three.proto';

package my.package;

option java_package = 'com.example.foo';
;
";

            RunTest(@"
{
  Package: my.package,
  Syntax: proto3,
  Imports: [
    {
      ImportPath: my/dir/one.proto
    },
    {
      ImportPath: my/dir/two.proto,
      ImportType: Weak
    },
    {
      ImportPath: my/dir/three.proto,
      ImportType: Public
    }
  ]
}", proto);
        }

        [Fact]
        public void ParseEnumDef() {
            string proto = @"
//Enum Comment
enum MyEnum {
  option allow_alias = true;
  //Enum 0 Comment
  EAA_UNSPECIFIED = 0;
  EAA_STARTED = 1;      //Enum 1 Comment
  EAA_RUNNING = 2 [(custom_option) = ""hello world""];
}
";

            RunTest(@"
{
  EnumDefs: [
    {
      Name: MyEnum,
      Values: [
        {
          Name: EAA_UNSPECIFIED,
          Comment: Enum 0 Comment
        },
        {
          Name: EAA_STARTED,
          Number: 1,
          Comment: Enum 1 Comment
        },
        {
          Name: EAA_RUNNING,
          Number: 2
        }
      ],
      Comment: Enum Comment
    }
  ]
}", proto);
        }

        [Fact]
        public void ParseBrokenIdentifier() {
            string proto = @"
message m {
    one . two
        .three f1 = 1;
    repeated . one . two f2 = 2;
}
";
            RunTest(@"
 {
   Messages: [
     {
       Name: m,
       Fields: [
         {
           Type: {
             Name: one.two.three
           },
           Number: 1,
           Name: f1
         },
         {
           Modifier: Repeated,
           Type: {
             Name: .one.two
           },
           Number: 2,
           Name: f2
         }
       ]
     }
   ]
 }", proto);
        }

        [Fact]
        public void ParseOneOf() {
            string proto = @"
message myMessage {
  //OneOf Comment
  oneof myOneOf {
    option ignoreMe = 'will be ignored';
    string either           = 3;         //Field 3 Comment
    . a . b . msgType or    = 4;
    ;
  }
}
";

            RunTest(@"
 {
   Messages: [
     {
       Name: myMessage,
       Fields: [
         {
           Fields: [
             {
               Type: {
                 Name: string
               },
               Number: 3,
               Name: either,
               Comment: Field 3 Comment
             },
             {
               Type: {
                 Name: .a.b.msgType
               },
               Number: 4,
               Name: or
             }
           ],
           Name: myOneOf,
           Comment: OneOf Comment
         }
       ]
     }
   ]
 }", proto);

        }

        [Fact]
        public void ParseMessage() {
            string proto = @"
//Message Comment
message myMessage {
  //Field 1 Comment
  int64 myInt = 1 [ opt1 = 'A', opt2 = 42 ];
  repeated string myString = 2;                 //Field 2 Comment
  optional string myOptional = 3;
  option ignoreMe = 'will be ignored';
  reserved 2, 15, 9 to 11;

  //Map Comment
  map<string, Project> myMap = 5;
  ;
}
";

            RunTest(@"
{
  Messages: [
    {
      Name: myMessage,
      Fields: [
        {
          Type: {
            Name: int64
          },
          Number: 1,
          Name: myInt,
          Comment: Field 1 Comment
        },
        {
          Modifier: Repeated,
          Type: {
            Name: string
          },
          Number: 2,
          Name: myString,
          Comment: Field 2 Comment
        },
        {
          Modifier: Optional,
          Type: {
            Name: string
          },
          Number: 3,
          Name: myOptional
        },
        {
          KeyType: {
            Name: string
          },
          ValueType: {
            Name: Project
          },
          Number: 5,
          Name: myMap,
          Comment: Map Comment
        }
      ],
      Comment: Message Comment
    }
  ]
}", proto);
        }

        [Fact]
        public void ParseMessageWithNested() {
            string proto = @"
message myMessage {
  //Nested Message Comment
  message myNested {}
  //Nested Enum Comment
  enum myNestedEnum {}
}
";

            RunTest(@"
{
  Messages: [
    {
      Name: myMessage,
      Messages: [
        {
          Name: myNested,
          Comment: Nested Message Comment
        }
      ],
      EnumDefs: [
        {
          Name: myNestedEnum,
          Values: [],
          Comment: Nested Enum Comment
        }
      ]
    }
  ]
}", proto);
        }

        [Fact]
        public void ParseService() {
            string proto = @"
//Service Comment            
service SearchService {
  option ignoreMe = 'will be ignored';
  //Rpc Comment
  rpc Search (a . SearchRequest) returns (. b . SearchResponse);
  rpc SearchStream (stream SearchRequest) returns (stream SearchResponse) { option a=1; option b=2; }
  ;
}";

            RunTest(@"
{
  Services: [
    {
      Name: SearchService,
      Rpcs: [
        {
          Name: Search,
          InputName: a.SearchRequest,
          OutputName: .b.SearchResponse,
          Comment: Rpc Comment
        },
        {
          Name: SearchStream,
          InputName: SearchRequest,
          IsInputStream: true,
          OutputName: SearchResponse,
          IsOutputStream: true
        }
      ],
      Comment: Service Comment
    }
  ]
}", proto);
        }        
        #endregion

        #region Protobuf 2 Specific
        [Fact]
        public void ParseProto2_Extensions() {
            string proto = @"
syntax = 'proto2';
message myMessage {
  extensions 100 to 199;
  extensions 4, 20 to max;
}
";

            RunTest(@"
 {
   Syntax: proto2,
   Messages: [
     {
       Name: myMessage
     }
   ]
 }", proto);
        }

        [Fact]
        public void ParseProto2_GroupField() {
            string proto = @"
syntax = 'proto2';
message myMessage {
  repeated group Result = 1 {
      required string url = 2;
      optional string title = 3;
      repeated string snippets = 4;
  }
}
";

            RunTest(@"
 {
   Syntax: proto2,
   Messages: [
     {
       Name: myMessage,
       Fields: [
         {
           Fields: [
             {
               Modifier: Required,
               Type: {
                 Name: string
               },
               Number: 2,
               Name: url
             },
             {
               Modifier: Optional,
               Type: {
                 Name: string
               },
               Number: 3,
               Name: title
             },
             {
               Modifier: Repeated,
               Type: {
                 Name: string
               },
               Number: 4,
               Name: snippets
             }
           ],
           Number: 1,
           Name: Result
         }
       ]
     }
   ]
 }", proto);
        }

        [Fact]
        public void ParseProto2_Extend() {
            string proto = @"
syntax = 'proto2';
extend Foo {
  optional int32 bar = 126;
}";

            RunTest(@"
 {
   Syntax: proto2,
   Extends: [
     {
       MessageType: Foo,
       Fields: [
         {
           Modifier: Optional,
           Type: {
             Name: int32
           },
           Number: 126,
           Name: bar
         }
       ],
       Message: {}
     }
   ]
 }", proto);
        }

        [Fact]
        public void ParseProto2_ExtendInMessage() {
            string proto = @"
syntax = 'proto2';
message myMessage {
  extend Foo {
    optional int32 bar = 126;
  }
}";

            RunTest(@"
 {
   Syntax: proto2,
   Messages: [
     {
       Name: myMessage,
       Extends: [
         {
           MessageType: Foo,
           Fields: [
             {
               Modifier: Optional,
               Type: {
                 Name: int32
               },
               Number: 126,
               Name: bar
             }
           ],
           Message: {}
         }
       ]
     }
   ]
 }", proto);
        }

        #endregion

        #region Utilities
        private void RunTest(string expected, string proto) {
            string actual = ReadProto(proto);
            expected = JsonFormattingUtils.DeleteFirstSpace(expected);

            if (actual != expected) {
                _output.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                _output.WriteLine(actual);      // We do this to get actual in full glory
                _output.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");

                Assert.Equal(expected, actual);
            }
        }

        private string ReadProto(string proto) {
            ProtobufTokenizer tokenizer = new ProtobufTokenizer(new StringReader(proto));
            #pragma warning disable 0162
            if (DUMP_TOKENS)
                _output.WriteLine(tokenizer.ToString());

            ProtobufParser parser = new ProtobufParser(tokenizer);
            PbFile file = parser.Parse();
            return JsonFormattingUtils.JsonPretty(file);
        }
        #endregion
    }
}
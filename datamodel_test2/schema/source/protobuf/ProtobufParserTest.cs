using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

namespace datamodel.schema.source.protobuf {
    public class ProtobufPerserTest {
        private const bool DUMP_TOKENS = false;

        private readonly ITestOutputHelper _output;

        public ProtobufPerserTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void ParseFileLevelProperties() {
            string proto = @"
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
      Path: my/dir/one.proto
    },
    {
      Path: my/dir/two.proto,
      ImportType: Weak
    },
    {
      Path: my/dir/three.proto,
      ImportType: Public
    }
  ]
}", proto);
        }

        [Fact]
        public void ParseEnumType() {
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
  EnumTypes: [
    {
      Name: MyEnum,
      Values: [
        {
          Name: EAA_UNSPECIFIED,
          Number: 0,
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
        public void ParseMessage() {
            string proto = @"
//Message Comment
message myMessage {
  //Field 1 Comment
  int64 myInt = 1 [ opt1 = 'A', opt2 = 42 ];
  repeated string myString = 2;                 //Field 2 Comment
  option ignoreMe = 'will be ignored';
  reserved 2, 15, 9 to 11;

  //OneOf Comment
  oneof myOneOf {
    option ignoreMe = 'will be ignored';
    string either = 3;                          //Field 3 Comment
    string or = 4;
    ;
  }

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
                Name: string
              },
              Number: 4,
              Name: or
            }
          ],
          Name: myOneOf,
          Comment: OneOf Comment
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
      Fields: [],
      Messages: [
        {
          Name: myNested,
          Fields: [],
          Comment: Nested Message Comment
        }
      ],
      EnumTypes: [
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
  rpc Search (SearchRequest) returns (SearchResponse);
  rpc SearchStream (stream SearchRequest) returns (stream SearchResponse);
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
          InputName: SearchRequest,
          OutputName: SearchResponse,
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

        private void RunTest(string expected, string proto) {
            string actual = ReadProto(proto).Trim().Replace("\"", "");
            expected = expected.Trim();

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
            File file = parser.Parse();

            return JsonConvert.SerializeObject(file,
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented,
                    Converters = new List<JsonConverter>() { new StringEnumConverter()},
                });
        }
    }
}
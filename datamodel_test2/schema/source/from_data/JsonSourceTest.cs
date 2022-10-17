using System;

using Xunit;
using Xunit.Abstractions;

using Newtonsoft.Json;

namespace datamodel.schema.source.from_data {
    public class JsonSourceTest {

        private readonly ITestOutputHelper _output;

        public JsonSourceTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void KeyIsValue() {
            Env.Configure();

            string text = @"{
    key_is_value: {
        one: {
            z: 7
        },
        two: {
            z: 8
        }
    }
}";

            JsonSource source = new JsonSource();
            source.Initialize(new Parameters(source, new string[] { 
                "raw=" + text,
                "paths-where-key-is-data=.key_is_value",
                }));


            string json = FromDataUtils.ToJasonNoQuotes(source, false);

            Assert.True(json.Replace(" ", "")
                .Contains(@"{
          DataType: string,
          Name: __key__,
          Labels: [
            {
              Name: Example,
              Value: one
            }
          ]
        }".Replace(" ", "")), json);
        }

        [Fact]
        public void BasicAttributes() {
            Env.Configure();
            string text = @"{
    a: 'A string',
    b: 12,
    c: 3.14,
    d: true,
    e: null,
    array_of_primitive: [ 'one', 'two', 'three' ]
}";

            JsonSource source = new JsonSource();
            source.Initialize(new Parameters(source, new string[] { 
                "raw=" + text,
                }));

            string json = FromDataUtils.ToJasonNoQuotes(source);

            // Console.WriteLine(json);
            Assert.Equal(@"{
  Models: {
    cluster1: {
      Name: cluster1,
      QualifiedName: cluster1,
      Levels: [
        cluster1
      ],
      AllProperties: [
        {
          DataType: String,
          Name: a
        },
        {
          DataType: Integer,
          Name: b
        },
        {
          DataType: Float,
          Name: c
        },
        {
          DataType: Boolean,
          Name: d
        },
        {
          Name: e
        },
        {
          DataType: []String,
          Name: array_of_primitive
        }
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ]
    }
  }
}", json);
        }

        [Fact]
        public void BasicObject() {
            Env.Configure();
            string text = @"{
    object: {
        aaa: ""String in object"",
        ccc: 18.7
    }
}";

            JsonSource source = new JsonSource();
            source.Initialize(new Parameters(source, new string[] { 
                "raw=" + text,
                }));

            string json = FromDataUtils.ToJasonNoQuotes(source);

            // Console.WriteLine(json);
            Assert.Equal(@"{
  Models: {
    cluster1: {
      Name: cluster1,
      QualifiedName: cluster1,
      Levels: [
        cluster1
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ]
    },
    cluster1.object: {
      Name: object,
      QualifiedName: cluster1.object,
      Levels: [
        cluster1
      ],
      AllProperties: [
        {
          DataType: String,
          Name: aaa
        },
        {
          DataType: Float,
          Name: ccc
        }
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ]
    }
  },
  Associations: [
    {
      OwnerSide: cluster1,
      OwnerMultiplicity: Aggregation,
      OtherSide: cluster1.object,
      OtherMultiplicity: ZeroOrOne
    }
  ]
}", json);
        }

        [Fact]
        public void BasicArray() {
            Env.Configure();
            string text = @"{
    array: [
        {
            aa: 'String in array',
            bb: 7
        },
        {
            aa: 'String in array 2',
            bb: 8
        }
    ]
}";
            JsonSource source = new JsonSource();
            source.Initialize(new Parameters(source, new string[] { 
                "raw=" + text,
                }));

            string json = FromDataUtils.ToJasonNoQuotes(source);

            // Console.WriteLine(json);
            Assert.Equal(@"{
  Models: {
    cluster1: {
      Name: cluster1,
      QualifiedName: cluster1,
      Levels: [
        cluster1
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ]
    },
    cluster1.array: {
      Name: array,
      QualifiedName: cluster1.array,
      Levels: [
        cluster1
      ],
      AllProperties: [
        {
          DataType: String,
          Name: aa
        },
        {
          DataType: Integer,
          Name: bb
        }
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 2
        }
      ]
    }
  },
  Associations: [
    {
      OwnerSide: cluster1,
      OwnerMultiplicity: Aggregation,
      OtherSide: cluster1.array,
      OtherMultiplicity: Many
    }
  ]
}", json);
        }
    }
}
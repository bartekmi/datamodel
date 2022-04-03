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

            TextSource text = TextSource.Text(@"{
    key_is_value: {
        one: {
            z: 7
        },
        two: {
            z: 8
        }
    }
}");

            JsonSource source = new JsonSource(text,
                new JsonSource.Options() {
                    PathsWhereKeyIsData = new string[] {
                        ".key_is_value",
                    }
                }
            );

            string json = FromDataUtils.ToJasonNoQuotes(source, false);

            Assert.True(json.Replace(" ", "")
                .Contains(@"{
          Labels: [
            {
              Name: Example,
              Value: one
            }
          ],
          Name: __key__,
          DataType: string
        }".Replace(" ", "")), json);
        }

        [Fact]
        public void BasicAttributes() {
            Env.Configure();
            TextSource text = TextSource.Text(@"{
    a: 'A string',
    b: 12,
    c: 3.14,
    d: true,
    e: null,
    array_of_primitive: [ 'one', 'two', 'three' ]
}");
            JsonSource source = new JsonSource(text);

            string json = FromDataUtils.ToJasonNoQuotes(source);

            // Console.WriteLine(json);
            Assert.Equal(@"{
  Models: {
    cluster1: {
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ],
      Name: cluster1,
      QualifiedName: cluster1,
      Levels: [
        cluster1
      ],
      AllColumns: [
        {
          Name: a,
          DataType: String
        },
        {
          Name: b,
          DataType: Integer
        },
        {
          Name: c,
          DataType: Float
        },
        {
          Name: d,
          DataType: Boolean
        },
        {
          Name: e
        },
        {
          Name: array_of_primitive,
          DataType: []String
        }
      ]
    }
  },
  Associations: []
}", json);
        }

        [Fact]
        public void BasicObject() {
            Env.Configure();
            TextSource text = TextSource.Text(@"{
    object: {
        aaa: ""String in object"",
        ccc: 18.7
    }
}");
            JsonSource source = new JsonSource(text);

            string json = FromDataUtils.ToJasonNoQuotes(source);

            // Console.WriteLine(json);
            Assert.Equal(@"{
  Models: {
    cluster1: {
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ],
      Name: cluster1,
      QualifiedName: cluster1,
      Levels: [
        cluster1
      ],
      AllColumns: []
    },
    cluster1.object: {
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ],
      Name: object,
      QualifiedName: cluster1.object,
      Levels: [
        cluster1
      ],
      AllColumns: [
        {
          Name: aaa,
          DataType: String
        },
        {
          Name: ccc,
          DataType: Float
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
TextSource text = TextSource.Text(@"{
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
}");
            JsonSource source = new JsonSource(text);

            string json = FromDataUtils.ToJasonNoQuotes(source);

            // Console.WriteLine(json);
            Assert.Equal(@"{
  Models: {
    cluster1: {
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ],
      Name: cluster1,
      QualifiedName: cluster1,
      Levels: [
        cluster1
      ],
      AllColumns: []
    },
    cluster1.array: {
      Labels: [
        {
          Name: Instance Count,
          Value: 2
        }
      ],
      Name: array,
      QualifiedName: cluster1.array,
      Levels: [
        cluster1
      ],
      AllColumns: [
        {
          Name: aa,
          DataType: String
        },
        {
          Name: bb,
          DataType: Integer
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
using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace datamodel.schema.source.from_data {
    public class JsonSourceKeyIsValueTest {

        [Fact]
        public void KeyIsValueBasic() {
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
                "paths-where-key-is-data=.key_is_value"
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
        public void KeyIsValueNested() {
            Env.Configure();

            string text = @"{
  defs: {
    ""a.b"": {
      props: {
        ver: {
          data: 1
        }
      }
    }
  }
}";

            JsonSource source = new JsonSource();
            source.Initialize(new Parameters(source, new string[] { 
                "raw=" + text,
                "paths-where-key-is-data=.defs.props"
                }));

            string json = FromDataUtils.ToJasonNoQuotes(source, false);
            Console.Write(json);
            
            AssertCollection(new string[] { 
                "cluster1",
                "cluster1.defs",
                "cluster1.defs.props",
              }, source.GetModels().Select(x => x.QualifiedName));
        }

        private void AssertCollection<T>(IEnumerable<T> expected, IEnumerable<T> actual) {
          Assert.Equal(expected.Count(), actual.Count());

          foreach (T item in expected)
            Assert.Contains(item, actual);
        }
 
        [Fact]
        // Let's throw in a cruve-ball... Not only is the key a value, what each
        // object item points to is an array, not another object.
        // Note that because of non-standard characters in the key, key_is_value object
        // will be taken as one where the keys are values
        public void KeyIsValueWithArray() {
            Env.Configure();

            string text = @"{
  key_is_value: {
    'http://a.com': [ 
      { z: 7 },
      { z: 8 }
    ],
    'http://b.com': [ 
      { z: 9 }
    ]
  }
}";

            // If you need to introspect the intermediate SDSS_Element layer
            // SDSS_Element root = JsonSource.GetRawInternal(text.GetText());
            // SampleDataKeyIsData.ConvertObjectsWhereKeyIsData(new JsonSource.Options(), root);
            // string output = FromDataUtils.ToJasonNoQuotes(root);
            // Console.Write(output);

            JsonSource source = new JsonSource();
            source.Initialize(new Parameters(source, new string[] { 
                "raw=" + text,
                }));
                
            string json = FromDataUtils.ToJasonNoQuotes(source, true);
            // Console.Write(json);

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
    cluster1.key_is_value: {
      Name: key_is_value,
      QualifiedName: cluster1.key_is_value,
      Levels: [
        cluster1
      ],
      AllProperties: [
        {
          DataType: string,
          Name: __key__
        }
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 2
        }
      ]
    },
    cluster1.key_is_value.Value: {
      Name: Value,
      QualifiedName: cluster1.key_is_value.Value,
      Levels: [
        cluster1
      ],
      AllProperties: [
        {
          DataType: Integer,
          Name: z
        }
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 3
        }
      ]
    }
  },
  Associations: [
    {
      OwnerSide: cluster1,
      OwnerMultiplicity: Aggregation,
      OtherSide: cluster1.key_is_value,
      OtherMultiplicity: Many
    },
    {
      OwnerSide: cluster1.key_is_value,
      OwnerMultiplicity: Aggregation,
      OtherSide: cluster1.key_is_value.Value,
      OtherMultiplicity: Many
    }
  ]
}", json);
        }
    }
}
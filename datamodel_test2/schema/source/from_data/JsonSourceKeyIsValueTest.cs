using System;

using Xunit;

namespace datamodel.schema.source.from_data {
    public class JsonSourceKeyIsValueTest {

        [Fact]
        public void KeyIsValueBasic() {
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
        // Let's throw in a cruve-ball... Not only is the key a value, what each
        // object item points to is an array, not another object.
        // Note that because of non-standard characters in the key, key_is_value object
        // will be taken as one where the keys are values
        public void KeyIsValueWithArray() {
            Env.Configure();

            TextSource text = TextSource.Text(@"{
  key_is_value: {
    'http://a.com': [ 
      { z: 7 },
      { z: 8 }
    ],
    'http://b.com': [ 
      { z: 9 }
    ]
  }
}");

            // If you need to introspect the intermediate SDSS_Element layer
            // SDSS_Element root = JsonSource.GetRawInternal(text.GetText());
            // SampleDataKeyIsData.ConvertObjectsWhereKeyIsData(new JsonSource.Options(), root);
            // string output = FromDataUtils.ToJasonNoQuotes(root);
            // Console.Write(output);

            JsonSource source = new JsonSource(text);
            string json = FromDataUtils.ToJasonNoQuotes(source, true);
            // Console.Write(json);

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
    cluster1.key_is_value: {
      Labels: [
        {
          Name: Instance Count,
          Value: 2
        }
      ],
      Name: key_is_value,
      QualifiedName: cluster1.key_is_value,
      Levels: [
        cluster1
      ],
      AllColumns: [
        {
          Name: __key__,
          DataType: string
        }
      ]
    },
    cluster1.key_is_value.Item: {
      Labels: [
        {
          Name: Instance Count,
          Value: 3
        }
      ],
      Name: Item,
      QualifiedName: cluster1.key_is_value.Item,
      Levels: [
        cluster1
      ],
      AllColumns: [
        {
          Name: z,
          DataType: Integer
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
      OtherSide: cluster1.key_is_value.Item,
      OtherMultiplicity: Many
    }
  ]
}", json);
        }
    }
}
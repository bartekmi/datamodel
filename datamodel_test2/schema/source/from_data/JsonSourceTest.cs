using System;
using System.Text.RegularExpressions;

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
            
            JsonSource source = new JsonSource("../../../schema/source/from_data/json_source_key_is_value.json", 
                new JsonSource.Options() {
                    PathsWhereKeyIsData = new string[] {
                        ".key_is_value",
                    }
                }
            );

            string json = JsonConvert.SerializeObject(
                source._source, 
                Formatting.Indented,
                new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

            Assert.True(json.Replace(" ", "")
                .Contains(@"{
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""one""
            }
          ],
          ""Name"": ""__key__"",
          ""DataType"": ""String""
        }".Replace(" ", "")), json);
        }

        [Fact]
        public void BasicProcessing() {
            Env.Configure();
            
            JsonSource source = new JsonSource("../../../schema/source/from_data/json_source_basic.json", 
                new JsonSource.Options());

            string json = JsonConvert.SerializeObject(
                source._source, 
                Formatting.Indented,
                new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

            Assert.Equal(@"{
  ""Models"": {
    ""cluster1"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""1""
        }
      ],
      ""Name"": ""cluster1"",
      ""QualifiedName"": ""cluster1"",
      ""Levels"": [
        ""cluster1""
      ],
      ""AllColumns"": [
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""A string""
            }
          ],
          ""Name"": ""a"",
          ""DataType"": ""String""
        },
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""12""
            }
          ],
          ""Name"": ""b"",
          ""DataType"": ""Integer""
        },
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""3.14""
            }
          ],
          ""Name"": ""c"",
          ""DataType"": ""Float""
        },
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""True""
            }
          ],
          ""Name"": ""d"",
          ""DataType"": ""Boolean""
        },
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": """"
            }
          ],
          ""Name"": ""e""
        },
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""one""
            }
          ],
          ""Name"": ""array_of_primitive"",
          ""DataType"": ""[]String""
        }
      ]
    },
    ""cluster1.array"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""2""
        }
      ],
      ""Name"": ""array"",
      ""QualifiedName"": ""cluster1.array"",
      ""Levels"": [
        ""cluster1""
      ],
      ""AllColumns"": [
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""String in array""
            }
          ],
          ""Name"": ""aa"",
          ""DataType"": ""String""
        },
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""7""
            }
          ],
          ""Name"": ""bb"",
          ""DataType"": ""Integer""
        }
      ]
    },
    ""cluster1.object"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""1""
        }
      ],
      ""Name"": ""object"",
      ""QualifiedName"": ""cluster1.object"",
      ""Levels"": [
        ""cluster1""
      ],
      ""AllColumns"": [
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""String in object""
            }
          ],
          ""Name"": ""aaa"",
          ""DataType"": ""String""
        },
        {
          ""Labels"": [
            {
              ""Name"": ""Example"",
              ""Value"": ""18.7""
            }
          ],
          ""Name"": ""ccc"",
          ""DataType"": ""Float""
        }
      ]
    }
  },
  ""Associations"": [
    {
      ""OwnerSide"": ""cluster1"",
      ""OwnerMultiplicity"": ""Aggregation"",
      ""OtherSide"": ""cluster1.array"",
      ""OtherMultiplicity"": ""Many""
    },
    {
      ""OwnerSide"": ""cluster1"",
      ""OwnerMultiplicity"": ""Aggregation"",
      ""OtherSide"": ""cluster1.object"",
      ""OtherMultiplicity"": ""ZeroOrOne""
    }
  ]
}", json);
        }

        [Fact]
        public void RegexMatch() {
            bool isMatch = SampleDataSchemaSource.PROP_NAME_REGEX.IsMatch("definitions");
            Assert.True(isMatch);
        }
    }
}
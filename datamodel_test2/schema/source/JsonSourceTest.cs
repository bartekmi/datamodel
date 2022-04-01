using System;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

using Newtonsoft.Json;

namespace datamodel.schema.source {
    public class JsonSourceTest {

        private readonly ITestOutputHelper _output;

        public JsonSourceTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void KeyIsValue() {
            Env.Configure();
            
            JsonSource source = new JsonSource("../../../schema/source/json_source_key_is_value.json", 
                new JsonSource.Options() {
                    PathsWhereKeyIsData = new string[] {
                        "root.key_is_value",
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
            
            JsonSource source = new JsonSource("../../../schema/source/json_source_basic.json", 
                new JsonSource.Options());

            string json = JsonConvert.SerializeObject(
                source._source, 
                Formatting.Indented,
                new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

            Assert.Equal(@"{
  ""Models"": {
    ""root"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""1""
        }
      ],
      ""Name"": ""root"",
      ""QualifiedName"": ""root"",
      ""Levels"": [],
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
    ""root.array"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""2""
        }
      ],
      ""Name"": ""array"",
      ""QualifiedName"": ""root.array"",
      ""Levels"": [],
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
    ""root.object"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""1""
        }
      ],
      ""Name"": ""object"",
      ""QualifiedName"": ""root.object"",
      ""Levels"": [],
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
      ""OwnerSide"": ""root"",
      ""OwnerMultiplicity"": ""Aggregation"",
      ""OtherSide"": ""root.array"",
      ""OtherMultiplicity"": ""Many""
    },
    {
      ""OwnerSide"": ""root"",
      ""OwnerMultiplicity"": ""Aggregation"",
      ""OtherSide"": ""root.object"",
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
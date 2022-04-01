using System;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

using Newtonsoft.Json;

using datamodel.schema.tweaks;

namespace datamodel.schema.source {
    public class JsonSourceClusterTest {

        [Fact]
        public void ClusterTest() {
            Env.Configure();

            JsonSource source = new JsonSource(new string[] {
                "../../../schema/source/json_source_cluster_1.json",
                "../../../schema/source/json_source_cluster_2.json",
                "../../../schema/source/json_source_cluster_3.json",
              }
            );

            TempSource data = source._source;
            foreach (Column column in data.AllColumns)
                column.Labels = null;   // Clean up output

            string json = JsonConvert.SerializeObject(
                data,
                Formatting.Indented,
                new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

            // Console.WriteLine(json);

            Assert.Equal(@"{
  ""Models"": {
    ""cluster1"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""2""
        }
      ],
      ""Name"": ""cluster1"",
      ""QualifiedName"": ""cluster1"",
      ""Levels"": [
        ""cluster1""
      ],
      ""AllColumns"": [
        {
          ""Name"": ""a"",
          ""DataType"": ""Integer""
        },
        {
          ""Name"": ""b"",
          ""DataType"": ""Integer"",
          ""CanBeEmpty"": true
        },
        {
          ""Name"": ""c"",
          ""DataType"": ""Integer"",
          ""CanBeEmpty"": true
        }
      ]
    },
    ""cluster1.obj1"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""2""
        }
      ],
      ""Name"": ""obj1"",
      ""QualifiedName"": ""cluster1.obj1"",
      ""Levels"": [
        ""cluster1""
      ],
      ""AllColumns"": [
        {
          ""Name"": ""n"",
          ""DataType"": ""String""
        }
      ]
    },
    ""cluster2"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""1""
        }
      ],
      ""Name"": ""cluster2"",
      ""QualifiedName"": ""cluster2"",
      ""Levels"": [
        ""cluster2""
      ],
      ""AllColumns"": [
        {
          ""Name"": ""z"",
          ""DataType"": ""Integer""
        }
      ]
    },
    ""cluster2.obj999"": {
      ""Labels"": [
        {
          ""Name"": ""Instance Count"",
          ""Value"": ""1""
        }
      ],
      ""Name"": ""obj999"",
      ""QualifiedName"": ""cluster2.obj999"",
      ""Levels"": [
        ""cluster2""
      ],
      ""AllColumns"": [
        {
          ""Name"": ""n"",
          ""DataType"": ""String""
        }
      ]
    }
  },
  ""Associations"": [
    {
      ""OwnerSide"": ""cluster1"",
      ""OwnerMultiplicity"": ""Aggregation"",
      ""OtherSide"": ""cluster1.obj1"",
      ""OtherMultiplicity"": ""ZeroOrOne""
    },
    {
      ""OwnerSide"": ""cluster2"",
      ""OwnerMultiplicity"": ""Aggregation"",
      ""OtherSide"": ""cluster2.obj999"",
      ""OtherMultiplicity"": ""ZeroOrOne""
    }
  ]
}", json);
        }
    }
}
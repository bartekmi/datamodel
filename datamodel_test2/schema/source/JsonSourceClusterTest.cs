using System;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

using Newtonsoft.Json;

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

            string json = JsonConvert.SerializeObject(
                source._source,
                Formatting.Indented,
                new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

            Console.WriteLine(json);
            Assert.Equal(@"", json);
        }
    }
}
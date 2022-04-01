using System;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

using Newtonsoft.Json;

namespace datamodel.schema.source {
    public class YamlSourceTest {

        private readonly ITestOutputHelper _output;

        public YamlSourceTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void Read() {
            Env.Configure();
            
            YamlSource source = new YamlSource("../../../schema/yaml_source.Yaml", 
                new YamlSource.Options() {
                    Title = "root",
                }
            );

            string json = JsonConvert.SerializeObject(source._source, Formatting.Indented);
            _output.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> RESULTS >>>>>>>>>>>>>>>>>>>>");
            _output.WriteLine(json);

            // Uncomment this line to see the results of the output above
            // Assert.False(true, "Fail on purpose");
        }
    }
}